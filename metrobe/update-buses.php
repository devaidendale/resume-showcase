<?php

// Updates bus data, called from AJAX in the main page, seamlessly updating the buses section.

require __DIR__.'/predis/autoload.php';
try
{
        $redis = new Predis\Client($single_server);
}
catch (Exception $e)
{
        echo "Couldn't connect to Redis";
        echo $e->getMessage();
}

echo '<table>';

$lengthBuses = $redis->get("busesCount");
//stores bus variables in bus array
for($i = 0; $i < $lengthBuses; $i++)
{
        $buses[$i]['lineName']      = $redis->hget("buses_".$i, "lineName");
        $buses[$i]['firstTime']     = $redis->hget("buses_".$i, "firstTime");
        $buses[$i]['secondTime']    = $redis->hget("buses_".$i, "secondTime");
        $buses[$i]['directionName'] = $redis->hget("buses_".$i, "directionName");
        $buses[$i]['locationName']  = $redis->hget("buses_".$i, "locationName");
        $buses[$i]['longitude']     = $redis->hget("buses_".$i, "longitude");
        $buses[$i]['latitude']      = $redis->hget("buses_".$i, "latitude");
        $disruptionsBuses      = $redis->hget("buses_".$i, "disruptions");


    $buses[$i]['grouping']      = -1;
}

// Group buses whose line names are the same.
for ($i = 0; $i < $lengthBuses; $i++)
{
    for ($j = 0; $j < $lengthBuses; $j++)
    {
        // Skip checking against itself.
        if ($i == $j)
            continue;

        // Iterate through buses for duplicate line names, in order to group them together.
        if ($buses[$i]['lineName'] == $buses[$j]['lineName'])
        {
            // Case: no groupings made.  This is the first entry that sees multiple line names, so it is the 'master' group.
            if ($buses[$i]['grouping'] == -1 && $buses[$j]['grouping'] == -1)
            {
                $buses[$i]['grouping'] = $i;
                $buses[$j]['grouping'] = $i;
            }
            // Case: the LHS match was made, but the RHS hasn't been yet.  So the RHS gets added to the master group.
            else if ($buses[$i]['grouping'] != -1 && $buses[$j]['grouping'] == -1)
            {
                $buses[$j]['grouping'] = $buses[$i]['grouping'];
            }
            // Case: the RHS match was made, but the LHS hasn't been yet.  So the LHS gets added to the master group.
            else if ($buses[$i]['grouping'] == -1 && $buses[$j]['grouping'] != -1)
            {
                $buses[$i]['grouping'] = $buses[$j]['grouping'];
            }
        }
    }
}

// SQL entries are automatically sorted.  No need to worry, son.
for ($i = 0; $i < $lengthBuses; $i++)
{
    // Entries that have a unique line name, and the 'master' group entry.
        echo "<tr>";
        echo "<th class='xs-col-12 title-heading bus-light text-white caps' colspan='3'>".$buses[$i]['lineName']."</th>";
        echo "</tr>";
        echo "<tr>";


             echo "<td class='xs-col-8 xs-border-bottom'><a href='http://maps.google.com?q=".$buses[$i]['latitude'].",".$buses[$i]['longitude']."'><h3 class='text-gray'>To ".$buses[$i]['directionName']." - <b>".$buses[$i]['locationName']."</b></h3></a> </td>";

             echo "<td class='xs-col-2 xs-border-bottom xs-text-center' style='background-color:#eaeaea;'><h2>".$buses[$i]['firstTime']."</h2></td>";
             echo "<td class='xs-col-2 xs-border-bottom xs-text-center' style='background-color:#dcdcdc;'><h2>".$buses[$i]['secondTime']."</h2></td>";
        echo "</tr>";

        // Get all subsequent entries that share a line name with the master entry.
        $grouped_bus_index = $buses[$i]['grouping'];

        if ($grouped_bus_index != -1)
        {
            while ($buses[$i + 1]['grouping'] == $grouped_bus_index && ($i + 1) < $lengthBuses)
            {
                echo "<tr>";
                                echo "<td class='xs-col-8 xs-border-bottom'><a href='http://maps.google.com?q=".$buses[$i + 1]['latitude'].",".$buses[$i + 1]['longitude']."'><h3 class='text-gray'>To ".$buses[$i + 1]['directionName']." - <b>".$buses[$i + 1]['locationName']."</b></h3></a> </td>";

                     echo "<td class='xs-col-2 xs-text-center xs-border-bottom' style='background-color:#eaeaea;'><h2>".$buses[$i + 1]['firstTime']."</h2></td>";
                     echo "<td class='xs-col-2 xs-text-center xs-border-bottom' style='background-color:#dcdcdc;'><h2>".$buses[$i + 1]['secondTime']."</h2></td>";
                echo "</tr>";
                $i++;
            }
        }

}
echo '</table>';

?>
