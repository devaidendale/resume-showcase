# La Trobe University - Metrobe

<b>The files presented here are for portfolio example only.</b>

Metrobe was an app developed by La Trobe students and later iterated on by a subsection of that group to incorporate La Trobe's own API which handled their internal student transport.

A detailed recount of the original development process was written by the front-end designer: https://medium.com/@SnazzyHam/the-year-long-process-to-change-the-way-public-transport-was-perceived-at-la-trobe-university-5c78a758f395

While the project is now defunct, as it has been incorporated into La Trobe's student suite, a snapshot of what it looked like can be viewed here: http://metrobe.info/

## Contents

### metrobe_d.php

The guts of the backend, the Metrobe daemon handled most of the data and data processing.  The NoSQL database, which held transient route info, was populated via a cronjob on my server, which was continually updated from PTV's API.  The SQL database, which held what routes to query PTV's API with, was intelligently inputted by an administrative interface built by one of the developers.

### buses.php

Handles all the bus data retrieved from the Metrobe daemon's processes.

### update-buses.php

One of the frontend -> backend interfaces, seamlessly grabbing bus data to present to the user.

### glider.php

Interfaces with La Trobe's own API for their internal transport.
