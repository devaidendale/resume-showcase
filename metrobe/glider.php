<?php

$glider_request      = $glider_base_url . "arrival_estimates";
$glider_result_json  = file_get_contents($glider_request);
$glider_result_array = json_decode($glider_result_json, true);

// Save the pertinent route data to an array, which gets saved to redis in the daemon.

$glider = [];
$glider_unsorted = [];

for($i = 0; $i < sizeof($glider_result_array['value']); $i++)
{
    $this_id       = $glider_result_array['value'][$i]['busstop_id'];
    $this_name     = $glider_result_array['value'][$i]['busstop_name'];
    $this_sequence = $glider_result_array['value'][$i]['busstop_seq'];
    $this_eta      = $glider_result_array['value'][$i]['eta'];

    array_push($glider_unsorted, $this_id, $this_name, $this_sequence, $this_eta);
    //array_push($glider, $this_id, $this_name, $this_sequence, $this_eta);
}

// Sort by sequence rather than index.
if(!empty($glider_unsorted))
  $lowest_sequence = $glider_unsorted[2]; // first index's sequence
$lowest_index = 0;
$sorted_indices = [];
for($i = 0; $i < sizeof($glider_unsorted); $i += $glider_vars)
{
    for($j = 0; $j < sizeof($glider_unsorted); $j += $glider_vars)
    {
        if($glider_unsorted[$j + 2] < $lowest_sequence)
        {
            $already_sorted = 0;
            for($k = 0; $k < sizeof($sorted_indices); $k++)
            {
                if($j == $sorted_indices[$k])
                  $already_sorted = 1;
            }
            if($already_sorted == 0)
            {
                $lowest_sequence = $glider_unsorted[$j + 2];
                $lowest_index = $j;
            }
        }
    }
    array_push($sorted_indices, $lowest_index);
    $lowest_sequence = 65534;
}

for($i = 0; $i < sizeof($sorted_indices); $i++)
{
    for($j = 0; $j < $glider_vars; $j++)
    {
        array_push($glider, $glider_unsorted[$sorted_indices[$i] + $j]);
    }
}
