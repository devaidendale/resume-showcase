<html>
  <head>
    <title>Wind Turbine Blade Calculator</title>
  </head>

  <!-- SET all variables and calculations. -->
    <?php
      $cl = 0.85;
      $alpha = 6;

      // BUFFER all variables for modifications.
        foreach($_POST[radMeasure] as $key => $value) {
          // SI defaulted.
          if($value == "Metric") $isMetric = true;
          if($value == "Imperial") $isImperial = true;
        }
        $blades = $_POST[blades];
        $tsr = $_POST[tsr];
        $bladeEff = $_POST[bladeEff];
        $bladeRad = $_POST[bladeRad];
        $windSpeed = $_POST[windSpeed];
      // FINISHED buffering all variables.


      // CALCULATE generator speed, torque, and power.
        function changeToSI() {
        	global $isMetric, $windSpeed, $isImperial, $bladeRad;
          if($isMetric == true) {
            $windSpeed = ((1000 * $windSpeed) / (60 * 60));
          }
          if($isImperial == true) {
          	$windSpeed = ((1609.344 * $windSpeed) / (60 * 60));
        	  $bladeRad = ($bladeRad / 3.2808399);
          }
        }
        changeToSI();
        
        $genPower = $bladeEff * 0.5 * 1.22 * 3.14159265358979 * pow($bladeRad, 2) * pow($windSpeed, 3);
        $genSpeed = (($windSpeed * $tsr) / $bladeRad);
        $genTorque = $genPower / $genSpeed;
        
        function changeFromSI() {
        	global $isMetric, $windSpeed, $isImperial, $bladeRad, $genSpeed;
        	if($isMetric == true) {
            $genSpeed = ($genSpeed * 60) / (2 * 3.14159265358979);
            $windspeed = ($windSpeed * 60 * 60) / 1000;
          }
          if($isImperial == true) {
          	$windSpeed = ($windSpeed * 60 * 60) / 1609.344;
        	  $bladeRad = ($bladeRad * 3.2808399);
        	  $genSpeed = ($genSpeed * 60) / (2 * 3.14159265358979);
          }
        }
        changeFromSI();
      // FINISHED calculating generator variables.


      $outputRadius01 = round((0.1 * $bladeRad), 3);
      $outputRadius02 = round((0.2 * $bladeRad), 3);
      $outputRadius03 = round((0.3 * $bladeRad), 3);
      $outputRadius04 = round((0.4 * $bladeRad), 3);
      $outputRadius05 = round((0.5 * $bladeRad), 3);
      $outputRadius06 = round((0.6 * $bladeRad), 3);
      $outputRadius07 = round((0.7 * $bladeRad), 3);
      $outputRadius08 = round((0.8 * $bladeRad), 3);
      $outputRadius09 = round((0.9 * $bladeRad), 3);
      $outputRadius10 = round((1 * $bladeRad), 3);

      $outputChord01 = round((5.6 * $bladeRad * $bladeRad) / ($blades * $cl * $tsr * $tsr * $outputRadius01), 3);
      $outputChord02 = round((5.6 * $bladeRad * $bladeRad) / ($blades * $cl * $tsr * $tsr * $outputRadius02), 3);
      $outputChord03 = round((5.6 * $bladeRad * $bladeRad) / ($blades * $cl * $tsr * $tsr * $outputRadius03), 3);
      $outputChord04 = round((5.6 * $bladeRad * $bladeRad) / ($blades * $cl * $tsr * $tsr * $outputRadius04), 3);
      $outputChord05 = round((5.6 * $bladeRad * $bladeRad) / ($blades * $cl * $tsr * $tsr * $outputRadius05), 3);
      $outputChord06 = round((5.6 * $bladeRad * $bladeRad) / ($blades * $cl * $tsr * $tsr * $outputRadius06), 3);
      $outputChord07 = round((5.6 * $bladeRad * $bladeRad) / ($blades * $cl * $tsr * $tsr * $outputRadius07), 3);
      $outputChord08 = round((5.6 * $bladeRad * $bladeRad) / ($blades * $cl * $tsr * $tsr * $outputRadius08), 3);
      $outputChord09 = round((5.6 * $bladeRad * $bladeRad) / ($blades * $cl * $tsr * $tsr * $outputRadius09), 3);
      $outputChord10 = round((5.6 * $bladeRad * $bladeRad) / ($blades * $cl * $tsr * $tsr * $outputRadius10), 3);

        // If Imperial is selected, convert (what would be) feet into inches.
      if($isImperial) {
      	$outputChord01 *= 12;
      	$outputChord02 *= 12;
      	$outputChord03 *= 12;
      	$outputChord04 *= 12;
      	$outputChord05 *= 12;
      	$outputChord06 *= 12;
      	$outputChord07 *= 12;
      	$outputChord08 *= 12;
      	$outputChord09 *= 12;
      	$outputChord10 *= 12;
      }

      $outputBeta01 = round((360 * atan(1 / ((1.5 * $outputRadius01 * $tsr) / $bladeRad))) / (2 * 3.14159265358979) - $alpha, 1);
      $outputBeta02 = round((360 * atan(1 / ((1.5 * $outputRadius02 * $tsr) / $bladeRad))) / (2 * 3.14159265358979) - $alpha, 1);
      $outputBeta03 = round((360 * atan(1 / ((1.5 * $outputRadius03 * $tsr) / $bladeRad))) / (2 * 3.14159265358979) - $alpha, 1);
      $outputBeta04 = round((360 * atan(1 / ((1.5 * $outputRadius04 * $tsr) / $bladeRad))) / (2 * 3.14159265358979) - $alpha, 1);
      $outputBeta05 = round((360 * atan(1 / ((1.5 * $outputRadius05 * $tsr) / $bladeRad))) / (2 * 3.14159265358979) - $alpha, 1);
      $outputBeta06 = round((360 * atan(1 / ((1.5 * $outputRadius06 * $tsr) / $bladeRad))) / (2 * 3.14159265358979) - $alpha, 1);
      $outputBeta07 = round((360 * atan(1 / ((1.5 * $outputRadius07 * $tsr) / $bladeRad))) / (2 * 3.14159265358979) - $alpha, 1);
      $outputBeta08 = round((360 * atan(1 / ((1.5 * $outputRadius08 * $tsr) / $bladeRad))) / (2 * 3.14159265358979) - $alpha, 1);
      $outputBeta09 = round((360 * atan(1 / ((1.5 * $outputRadius09 * $tsr) / $bladeRad))) / (2 * 3.14159265358979) - $alpha, 1);
      $outputBeta10 = round((360 * atan(1 / ((1.5 * $outputRadius10 * $tsr) / $bladeRad))) / (2 * 3.14159265358979) - $alpha, 1);
      
    ?>
  <!-- FINISHED setting all variables and calculations. -->

  <body><center>
    <h1>Wind Turbine Blade Calculator - Results</h1>
    <br>

    <font face = "courier new">
      
      <table border = 0 cellspacing = 1>
        <td>
          <center><img src="PICTURE2.jpg"></img></center>

          <table border = 0 cellspacing = 4>

            <tr>
              <td>
                <form><input type="button" VALUE="Back" onClick="history.go(-1);return true;"></form>  
              </td>
            </tr>

            <tr>
              <td>
                <?php echo round($genSpeed, 1) ?>
              </td>
              <td>
                <?php
                  if($isMetric || $isImperial) echo "Generator Speed (rpm)";
                  else                         echo "Generator Speed (rad/sec)";
                ?>
              </td>
            </tr>

            <tr>
              <td>
                <?php echo round($genTorque, 2) ?>
              </td>
              <td>
                Generator Torque (N.m)
              </td>
            </tr>

            <tr>
              <td>
                <?php echo round($genPower, 1) ?>
              </td>
              <td>
                Generator Power (Watts)
              </td>
            </tr>

          </table>

        </td>
      <td>
        <img src="PICTURE1.gif"></img>
      </td>

    </table>

    <table border = 1 cellspacing = 1>

      <tr>
        <td bgcolor="cccccc">
          <?php
            if($isImperial) echo "Radius (ft)";
            else            echo "Radius (m)";
          ?>
        </td>
        <td bgcolor="cccccc">
          <?php
            if($isImperial) echo "Chord (in)";
            else            echo "Chord (m)";
          ?>
        </td>
        <td bgcolor="cccccc">
          ß (°)
        </td>
      </tr>

      <tr>
        <td>
          <?php echo $outputRadius01 ?>
        </td>
        <td>
          <?php echo $outputChord01 ?>
        </td>
        <td>
          <?php echo $outputBeta01 ?>
        </td>
      </tr>
      <tr>
        <td>
          <?php echo $outputRadius02 ?>
        </td>
        <td>
          <?php echo $outputChord02 ?>
        </td>
        <td>
          <?php echo $outputBeta02 ?>
        </td>
      </tr>
      <tr>
        <td>
          <?php echo $outputRadius03 ?>
        </td>
        <td>
          <?php echo $outputChord03 ?>
        </td>
        <td>
          <?php echo $outputBeta03 ?>
        </td>
      </tr>
      <tr>
        <td>
          <?php echo $outputRadius04 ?>
        </td>
        <td>
          <?php echo $outputChord04 ?>
        </td>
        <td>
          <?php echo $outputBeta04 ?>
        </td>
      </tr>
      <tr>
        <td>
          <?php echo $outputRadius05 ?>
        </td>
        <td>
          <?php echo $outputChord05 ?>
        </td>
        <td>
          <?php echo $outputBeta05 ?>
        </td>
      </tr>
      <tr>
        <td>
          <?php echo $outputRadius06 ?>
        </td>
        <td>
          <?php echo $outputChord06 ?>
        </td>
        <td>
          <?php echo $outputBeta06 ?>
        </td>
      </tr>
      <tr>
        <td>
          <?php echo $outputRadius07 ?>
        </td>
        <td>
          <?php echo $outputChord07 ?>
        </td>
        <td>
          <?php echo $outputBeta07 ?>
        </td>
      </tr>
      <tr>
        <td>
          <?php echo $outputRadius08 ?>
        </td>
        <td>
          <?php echo $outputChord08 ?>
        </td>
        <td>
          <?php echo $outputBeta08 ?>
        </td>
      </tr>
      <tr>
        <td>
          <?php echo $outputRadius09 ?>
        </td>
        <td>
          <?php echo $outputChord09 ?>
        </td>
        <td>
          <?php echo $outputBeta09 ?>
        </td>
      </tr>
      <tr>
        <td>
          <?php echo $outputRadius10 ?>
        </td>
        <td>
          <?php echo $outputChord10 ?>
        </td>
        <td>
          <?php echo $outputBeta10 ?>
        </td>
      </tr>

    <?php
      if($isImperial) echo "<b><i>Caveat: Imperial is not 100% accurate.</i></b>";
    ?>
    
    </table>
    
  <img src="PICTURE3.jpg"></img>
  <br>

  <!-- CREATE the graph of Power vs. Wind Speed. -->
    <?php
    
      function graphGetPower($index) {
        global $bladeEff, $bladeRad;
        changeToSI();
        $result = $bladeEff * 0.5 * 1.22 * 3.14159265358979 * pow($bladeRad, 2) * pow($index, 3);
        changeFromSI();
        return $result;
      }
    
      // Create each power over wind speed component from 1 to 20.
      $graphPower = range(1,20);
      for($index = 1; $index <= 20; $index += 1) {
      	$graphPower[$index] = graphGetPower($index);
      }
      
      $theChart = "http://chart.apis.google.com/chart?chs=450x300".
                  "&chd=t:";
                  
                  // Append each value into the graph.
                  for($index = 1; $index <= 20; $index += 1) {
                  	$theChart .= $graphPower[$index];
                  	if($index != 20) $theChart .= ",";
                  }
      $theChart .= "&cht=lc".                            // Designate line graph.
                  "&chtt=Power+versus+Wind".             // Title of the graph.
                  "&chds=0,".max($graphPower).           // The min and max limits.
                  "&chxt=x,y,x,y".                       // Define two sets of x and y axis.
                  "&chxr=0,0,20|1,0,".max($graphPower).  // Define axis ranges.
                  "&chxl=2:|Metres+per+Second|3:|Watts"; // Labels of the axis.
                  "&chxp=2,50|3,50";                     // Centre those labels.
                  
      echo "<center><img src=\"".$theChart."></img></center>";

    ?>      
  <!-- END creating the graph. -->


  <!-- WRITE the query to a data file. -->
    <?php

      $thisYear = date("Y");
      $thisMonth = date("M");

      function VisitorIP() { 
        if(isset($_SERVER['HTTP_X_FORWARDED_FOR']))
          $theIp=$_SERVER['HTTP_X_FORWARDED_FOR'];
        else $theIp=$_SERVER['REMOTE_ADDR'];
        return trim($theIp);
      }
      
      $queryFile = "BC-".$thisMonth.$thisYear.".log";
      $openFile = fopen($queryFile, 'a') or die("ERROR: Cannot open user query record file.");

      $stringData = "USER QUERY from IP ADDRESS: " . VisitorIP() . "\n" .
                    "Date and Time of Query: " . date("h:ia D d M Y") . "\n" .
                    "Measurement Type: ";
      foreach($_POST[radMeasure] as $key => $value) {
        $stringData .= $value . " ";
      }
      $stringData .="\n" .
                    "Number of Blades: " . $_POST[blades] . "\n" .
                    "TSR: " . $_POST[tsr] . "\n" .
                    "Blade Efficiency: " . $_POST[bladeEff] . "\n" .
                    "Blade Radius: " . $_POST[bladeRad] . "\n" .
                    "Wind Speed: " . $_POST[windSpeed] . "\n\n";
      fwrite($openFile, $stringData);
      fclose($openFile);
    ?>
  <!-- END writing user query to data file. -->

  </center></body>
</html>
