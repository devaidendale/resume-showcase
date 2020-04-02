<?php

// (October 2017 route fix)
// If a previous cronjob is still fixing the routes, skip this interval instance update.
$route_health = file_get_contents('route-health.dat');

echo "route health: " . $route_health . " \n";

if(strcmp($route_health, "PROCESSING") == 0)
{
    exit(0);
}

// Grab the routes we want to query the PTV API from the SQL database.
// The queries are sorted by the stop_name, this is so they're displayed properly under potentially grouped headings on the front-end regardless of admin error.  Also, it looks neater being alphanumerically sorted.
$busquery   = "SELECT * FROM buses ORDER BY stop_name";
$tramquery  = "SELECT * FROM trams ORDER BY stop_name";
$trainquery = "SELECT * FROM trains ORDER BY stop_name";

// These queries are for the string truncations, which can be added to the admin console.
$bus_trunc_query   = "SELECT * FROM string_replacements WHERE transport_type = 'bus'";
$tram_trunc_query  = "SELECT * FROM string_replacements WHERE transport_type = 'tram'";
$train_trunc_query = "SELECT * FROM string_replacements WHERE transport_type = 'train'";

$db = mysqli_connect('localhost','root','i-phuck^y0u','metrobe')
      or die('Error connecting to MySQL server.');

if (!$db)
{
    die("Connection failed: " . mysqli_connect_error());
}

$result_bus   = mysqli_query($db, $busquery);
$result_tram  = mysqli_query($db, $tramquery);
$result_train = mysqli_query($db, $trainquery);

$result_bus_trunc   = mysqli_query($db, $bus_trunc_query);
$result_tram_trunc  = mysqli_query($db, $tram_trunc_query);
$result_train_trunc = mysqli_query($db, $train_trunc_query);

$sizeof_bus   = mysqli_num_rows($result_bus);
$sizeof_tram  = mysqli_num_rows($result_tram);
$sizeof_train = mysqli_num_rows($result_train);

$sizeof_bus_trunc   = mysqli_num_rows($result_bus_trunc);
$sizeof_tram_trunc  = mysqli_num_rows($result_tram_trunc);
$sizeof_train_trunc = mysqli_num_rows($result_train_trunc);

// This variable stores how many columns are stored in redis.  For now, a static number is best.
// We could ostensibly make it read redis but this is needlessly complicated -- you'd have to use an array of hashes.
$bus_vars    = 8;
$tram_vars   = 5;
$train_vars  = 5;
$glider_vars = 4;

// Populate the queries to be used in the request modules.
while($busrow = $result_bus->fetch_row())
{
    $sql_buses[] = $busrow;
}
while($tramrow = $result_tram->fetch_row())
{
    $sql_trams[] = $tramrow;
}
while($trainrow = $result_train->fetch_row())
{
    $sql_trains[] = $trainrow;
}

// Populate the string truncation queries as well.
while($bus_trunc_row = $result_bus_trunc->fetch_row())
{
    $sql_bus_truncs[] = $bus_trunc_row;
}
while($tram_trunc_row = $result_tram_trunc->fetch_row())
{
    $sql_tram_truncs[] = $tram_trunc_row;
}
while($train_trunc_row = $result_train_trunc->fetch_row())
{
    $sql_train_truncs[] = $train_trunc_row;
}

// End SQL.
// (October 2017 route fix: mysqli_close($db) doesn't occur until later now, because of potential route fixes.)


// Open up Redis to save results from PTV API calls.
require '/root/predis/autoload.php';

try
{
    // '0' or a null value -> localhost's server information.
    $redis = new Predis\Client(0);
}
catch (Exception $e)
{
    echo "Couldn't connect to Redis";
    echo $e->getMessage();
}

// Static vars and developer info for API calls.
$ptv_limit    = 3;
$ptv_currDate = gmDate("Y-m-d\TH:i:s\Z");
$ptv_devid    = "1000714";
$ptv_secret   = "c386ea7f-f543-11e5-a65e-029db85e733b";
$ptv_base_url = "http://timetableapi.ptv.vic.gov.au";

// Make the API calls and build subsequent arrays.
echo "doing trains (" . $route_health . ")\n";
include 'trains.php';
echo "doing buses (" . $route_health . ")\n";
include 'buses.php';
echo "doing trams (" . $route_health . ")\n";
include 'trams.php';

// (October 2017 route fix)
// Check if any routes are broken before proceeding.
// WARNING: if PTV delete a route for some reason, this will screw the whole server up.  :D
if(strcmp($route_health, "BROKEN") == 0)
{
echo "ham";
    // Set flag for subsequent cronjobs of the metrobe daemon to exit until this instance is complete.
    $route_health = "PROCESSING";
    file_put_contents('route-health.dat', $route_health);

    // Update old route info with new route info.

    // Buses:
    $busquery        = "SELECT * FROM busstops";
    $busquery_result = mysqli_query($db, $busquery);
    while($row = mysqli_fetch_assoc($busquery_result))
    {
        // For each busstop, a broad next departures API request is called.
        // Each result from the data then is filtered to match the entries we already have in the buses table.
        // Then, the direction_id of each entry is simply updated, because this is ALWAYS what breaks it.

        $bus_mode = 2;
        $bus_stopid = $row['stop_id'];
        $ptv_limit = 1;

        $ptv_request = "/v2/mode/$bus_mode/stop/$bus_stopid/departures/by-destination/limit/$ptv_limit" . '?devid=' . $ptv_devid;

        $ptv_signature = hash_hmac('sha1', $ptv_request, $ptv_secret);
        $url = $ptv_base_url . $ptv_request . '&signature=' . $ptv_signature;

        $ptv_result = file_get_contents($url);

        $ptv_json   = json_decode($ptv_result, true);

        for ($i = 0; $i < count($ptv_json['values']); $i++)
        {
            $stopid             = $ptv_json['values'][$i]['platform']['stop']['stop_id'];
            $line_id            = $ptv_json['values'][$i]['platform']['direction']['line']['line_id'];
            $fixed_direction_id = $ptv_json['values'][$i]['platform']['direction']['direction_id'];
            $direction_name_key = $ptv_json['values'][$i]['platform']['direction']['direction_name'];

            // Match our bus(es) first; we only care about the bus(es) we have on this stop.
            $check = "SELECT * FROM buses WHERE line_id = $line_id AND stop_id = $stopid AND direction_name = '$direction_name_key'";
            $result_check = mysqli_query($db, $check);

            while($row2 = mysqli_fetch_assoc($result_check))
            {
                // Update direction_id of the entry to the fixed one.
                $busquery_fix = "UPDATE buses
                                 SET direction_id = $fixed_direction_id
                                 WHERE line_id = $line_id
                                 AND stop_id = $stopid
                                 AND direction_name = '$direction_name_key'";

                $busquery_fix_result = mysqli_query($db, $busquery_fix);
            }

        }
    }

    // Trains: Will need broken trains to ensure this works.  There have never been any broken trains, so it can't be tested.
    // Trams: Will need broken trams to ensure this works.  There have almost never been any broken trams, so it can't be tested.

    // Set flag to show that routes are now healthy again.
    $route_health = "HEALTHY";
    file_put_contents('route-health.dat', $route_health);

    exit(0);
}

mysqli_close($db);

// Static vars for the Glider API calls.
$glider_base_url = "https://campusglider.info/latrobe_glider/api/v1.0/";
$glider_currDate = gmDate("Y-m-d\TH:i:s\Z");

// Make the Glider API calls and build subsequent arrays.
// (January 2018 Update): Removed glider, the server is either down or it has been discontinued.  Spamming root inbox with warnings.
//include 'glider.php';

// Write that data to redis database.
try
{
    // Flush old data to remove previous update.
    $redis->flushall();

    for($i = 0; $i < (count($trains) / $train_vars); $i++)
    {
        $redis->hmset("trains_" . $i,
                          "lineName",      $trains[0 + ($i * $train_vars)],
                          "firstTime",     $trains[1 + ($i * $train_vars)],
                          "secondTime",    $trains[2 + ($i * $train_vars)],
                          "directionName", $trains[3 + ($i * $train_vars)],
                          "disruptions",   $trains[4 + ($i * $train_vars)]
                     );
    }
    $redis->set("trainsCount", (count($trains) / $train_vars));

    for($i = 0; $i < (count($trams) / $tram_vars); $i++)
    {
        $redis->hmset("trams_" . $i,
                          "lineName",      $trams[0 + ($i * $tram_vars)],
                          "firstTime",     $trams[1 + ($i * $tram_vars)],
                          "secondTime",    $trams[2 + ($i * $tram_vars)],
                          "directionName", $trams[3 + ($i * $tram_vars)],
                          "disruptions",   $trams[4 + ($i * $tram_vars)]
                     );
    }
    $redis->set("tramsCount", (count($trams) / $tram_vars));

    for($i = 0; $i < (count($buses) / $bus_vars); $i++)
    {
        $redis->hmset("buses_" . $i,
                          "lineName",      $buses[0 + ($i * $bus_vars)],
                          "firstTime",     $buses[1 + ($i * $bus_vars)],
                          "secondTime",    $buses[2 + ($i * $bus_vars)],
                          "directionName", $buses[3 + ($i * $bus_vars)],
                          "locationName",  $buses[4 + ($i * $bus_vars)],
                          "longitude",     $buses[5 + ($i * $bus_vars)],
                          "latitude",      $buses[6 + ($i * $bus_vars)],
                          "disruptions",   $buses[7 + ($i * $bus_vars)]
                     );
    }
    $redis->set("busesCount", (count($buses) / $bus_vars));
/* (January 2018 Update): Removed, see above reasons.
    for($i = 0; $i < (count($glider) / $glider_vars); $i++)
    {
        $redis->hmset("glider_" . $i,
                          "id",       $glider[0 + ($i * $glider_vars)],
                          "name",     $glider[1 + ($i * $glider_vars)],
                          "sequence", $glider[2 + ($i * $glider_vars)],
                          "eta",      $glider[3 + ($i * $glider_vars)]
                     );
    }
    $redis->set("gliderCount", (count($glider) / $glider_vars));
*/
}
catch (Exception $e)
{
    echo "Error: " . $e->getMessage();
}

?>
