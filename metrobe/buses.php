<?php
//make all requests and store return
for ($i = 0; $i < $sizeof_bus; $i++)
{
    list($ptv_mode, $ptv_line_id, $ptv_stop_id, $ptv_direction_id) = $sql_buses[$i];

    $ptv_request = "/v2/mode/$ptv_mode/line/$ptv_line_id/stop/$ptv_stop_id/directionid/$ptv_direction_id/departures/all/limit/$ptv_limit" .
                   '?for_utc' . $ptv_currDate . '&devid=' . $ptv_devid;

    $ptv_signature = hash_hmac('sha1', $ptv_request, $ptv_secret);
    $url           = $ptv_base_url . $ptv_request . '&signature=' . $ptv_signature;

    $ptv_result   = file_get_contents($url);
    $ptv_json[$i] = json_decode($ptv_result, true);
}

$buses = [];
$disruptions_string = ""; // Because it may have data in it from other files.

// Save the pertinent route data to an array, which gets saved to redis in the daemon.
for($i = 0; $i < $sizeof_bus; $i++)
{
    // Capture the very rare times PTV changes the route IDs, which requires admin intervention.
    if(empty($ptv_json[$i]['values']))
    {
        $timeBetween  = "<b>Please</b>";
        $timeBetween2 = "<b>Wait...</b>";
        $this_location_name = "<b>Updating this route!</b>";

        // (October 2017 route fix)
        // Add flag that the route is broken.
        $route_health = "BROKEN";
    }
    // Normal function.
    else
    {
        $this_first_departure  = $ptv_json[$i]['values'][0]['time_timetable_utc'];
        $this_second_departure = $ptv_json[$i]['values'][1]['time_timetable_utc'];
        $this_location_name    = $ptv_json[$i]['values'][0]['platform']['stop']['location_name'];
        $this_longitude        = $ptv_json[$i]['values'][0]['platform']['stop']['lon'];
        $this_latitude         = $ptv_json[$i]['values'][0]['platform']['stop']['lat'];

        //Time conversion
        $arrTime  = strtotime($this_first_departure);
        $arrTime2 = strtotime($this_second_departure);
        $currTime = strtotime($ptv_currDate);

        // Store disruption data, if there are any.
        if(!empty($ptv_json[$i]['values'][0]['disruptions']))
        {
            $disruptions = $ptv_json[$i]['values'][0]['disruptions'];
            for($j = 0; $j < count($disruptions); $j++)
            {
                // Only add if it hasn't been already added, else it will duplicate (since we iterate through them all this is likely to occur).
                // This means that the earliest entries will contain the disruption data for that line.
                $disruptions_entry = $disruptions[$j]['title'] . " ... " . $disruptions[$j]['description'] . " ... ";
                if(strpos($disruptions_string, $disruptions_entry) !== false)
                {
                    // NOP
                }
                else
                {
                    $disruptions_string = $disruptions_string . $disruptions_entry;
                }
            }
        }

        // Check if the first departure was over 1.5 minutes ago, and is still being propagated by the slow-to-update PTV API.
        // This prevents the 'Now' message being displayed for long periods of time, which occurs very often (if not always).
        if ($arrTime - $currTime < -90)
        {
            // Skip ahead one, so we read position 2 and 3, rather than 1 and 2.
            $this_first_departure  = $ptv_json[$i]['values'][1]['time_timetable_utc'];
            $this_second_departure = $ptv_json[$i]['values'][2]['time_timetable_utc'];

            $arrTime  = strtotime($this_first_departure);
            $arrTime2 = strtotime($this_second_departure);
        }

        //Logic for displaying minutes/hours/etc
        if ($arrTime > $currTime)
        {
            $timeBetween = ($arrTime - $currTime)/60;
            if ($timeBetween > 60)
            {
                $timeBetween = "> " . intval($timeBetween/60) . " hrs";
            }
            else
            {
                $timeBetween = ceil($timeBetween) . " mins";
            }
        }
        else if (($arrTime - $currTime < 0) && ($arrTime - $currTime > -60))
        {
            $timeBetween = "<b><span class='text-green'>Now</span><b>";
        }
        else
        {   // Opntional message, removed due to user testing.
            $timeBetween = "<b><span class='text-green'>Now</span><b>";
        }

        if ($arrTime2 > $currTime)
        {
            $timeBetween2 = ($arrTime2 - $currTime)/60;
            if ($timeBetween2 > 60)
            {
                $timeBetween2 = "> " . intval($timeBetween2/60) . " hrs";
            }
            else
            {
                $timeBetween2 = ceil($timeBetween2) . " mins";
            }
        }
        else if (($arrTime2 - $currTime < 0) && ($arrTime2 - $currTime > -60))
        {
            $timeBetween2 = "<b><span class='text-green'>Now</span><b>";
        }
        else
        {   // Optional message, removed due to user testing.
            $timeBetween2 = "<b><span class='text-green'>Now</span><b>";
        }
    }

                    // line_name (stop_name in sql)                    direction_name
    array_push($buses, $sql_buses[$i][4], $timeBetween, $timeBetween2, $sql_buses[$i][5], $this_location_name, $this_longitude, $this_latitude, $disruptions_string);
}

// Apply any bus string truncations that were set.
for($i = 0; $i < $sizeof_bus_trunc; $i++)
{
    if($sql_bus_truncs[$i][1] == 'line_name')
    {   // This truncation is for the line_name.
        for($j = 0; $j < $sizeof_bus; $j++)
        {
                                                    // matching string         replacing string
            $buses[0 + ($j * $bus_vars)] = str_replace($sql_bus_truncs[$i][2], $sql_bus_truncs[$i][3], $buses[0 + ($j * $bus_vars)]);
        }
    }
    else if($sql_bus_truncs[$i][1] == 'location_name')
    {
        // This truncation is for the location_name.
        for($j = 0; $j < $sizeof_bus; $j++)
        {
                                                    // matching string         replacing string
            $buses[4 + ($j * $bus_vars)] = str_replace($sql_bus_truncs[$i][2], $sql_bus_truncs[$i][3], $buses[4 + ($j * $bus_vars)]);
        }
    }
    else if($sql_bus_truncs[$i][1] == 'direction_name')
    {   // This truncation is for the direction_name.
        for($j = 0; $j < $sizeof_bus; $j++)
        {
                                                    // matching string         replacing string
            $buses[3 + ($j * $bus_vars)] = str_replace($sql_bus_truncs[$i][2], $sql_bus_truncs[$i][3], $buses[3 + ($j * $bus_vars)]);
        }
    }

}

?>
