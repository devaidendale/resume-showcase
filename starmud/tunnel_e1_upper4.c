#include "../../nsewers.h"

inherit ROOM;

int rand_monsters;
int count_monsters;
int event_monsters = 0;

int state_tactivate_entered;
int state_tactivate_exited;
string t_question;
string t_desc;
int uu,vv,ii,bb,nn,rq;
string nn_toStr;
string ii_toStr;

void set_item_descs() {
  set("item_desc", ([
      "nepos sewers"            : "#sewers",
      "sewer"                   : "#sewers",
      "sewers"                  : wrap("\
These are the sewers of Nepos, the proverbial underworld of Nepos. A 
criss-cross of huge tunnels, crime and experiments gone wrong."),
      "nepos"                   : wrap("\
Nepos... the planet, where these sewers are located. Does someone have 
amnesia?"),
      "tunnel"                  : "#section",
      "tunnels"                 : "#sewers",
      "huge tunnel"             : "#sewers",
      "section"                 : wrap("\
This section of the Nepos sewers appears to lead north and south, with a 
connecting tunnel to the east."),
      "dark metallic walls"     : "#wall",
      "dark walls"              : "#wall",
      "metallic walls"          : "#wall",
      "walls"                   : "#wall",
      "wall"                    : wrap("\
The walls of the sewers are fairly nondescript sections of dark metal. They 
look to be very dense, quite possibly holding up the foundation of the huge 
city above. The walls have many blue holograms attached to them, and a 
terminal lies in the northeast corner."),
      "ground"                  : wrap("\
The ground appears to be a mostly-solid miasma of filth, cybernetics, and 
failed experiments. It is not a pleasant sight."),
      "filth"                   : wrap("\
Carbon-based refuse."),
      "cybernetics"             : wrap("\
Silicon-based refuse and garbage."),
      "failed experiments"      : "#experiments",
      "experiments"             : wrap("\
Mostly lumps of discarded protoplasm or carbon entities, too disfigured or 
damaged to identify. Think of all the horrors of early 20th century Terra 
and magnify it with the advanced technology of Nepos."),
      "blue holograms"          : "#holograms",
      "blue hologram"           : "#holograms",
      "hologram"                : "#holograms",
      "hologram"                : wrap("\
The holograms are blue and represent something that looks cross between an 
ankh and a pentagram. They are more centred around the northeast corner of 
the room."),
      "northeast corner"        : "#corner",
      "corner"                  : wrap("\
Amidst the blue holograms, there lies a terminal in the northeast corner of 
the room."),
      "message"                 : "#terminal",
      "terminal"                : wrap("\
" + t_desc),
      "connecting tunnel"       : wrap("\
The connecting tunnel to the east connects this upper-east section of tunnel 
to other sections."),
  ]));
}

void create () {
  set("roof", 1);
  set("light", 1);
  set("no_teleport", 1);
  set("short", "a Nepos sewer tunnel :: upper-east section");
  set("long", wrap("\
The sewers of Nepos are vast, and its tunnels are huge; this section is no 
exception. The dark, metallic walls rise up many metres, and the ground, 
while mostly solid, looks as dangerous as it is pungent. The huge tunnel 
stretches further to the north and south, with a connecting tunnel attached 
towards the east. The walls are covered in blue holograms. There is a 
terminal in the northeast corner."));

  state_tactivate_entered  = 0;
  state_tactivate_exited   = 1;
  t_question               = "No question has been constructed.";
  t_desc                   = "While the terminal looks like it has been " +
                           "frequently used, the screen only presents a " + 
                  "simple prompt. Perhaps one could 'activate terminal'?";

  set_item_descs();

  rand_monsters = random(4);
  for (count_monsters = 0; count_monsters < rand_monsters; count_monsters++)
    add("objects", ({ NS_MON "cybmon" }) );
  rand_monsters = random(4);
  for (count_monsters = 0; count_monsters < rand_monsters; count_monsters++)
    add("objects", ({ NS_MON "fexperi" }) );
  rand_monsters = random(3);
  for (count_monsters = 0; count_monsters < rand_monsters; count_monsters++)
    add("objects", ({ NS_MON "sstech" }) );
  rand_monsters = random(5);
  for (count_monsters = 0; count_monsters < rand_monsters; count_monsters++)
    add("objects", ({ NS_MON "ssdoctor" }) );
  rand_monsters = random(3);
  for (count_monsters = 0; count_monsters < rand_monsters; count_monsters++)
    add("objects", ({ NS_MON "ssprof" }) );

  set("smell", "It smells fetid and horrible, like a sewer should be.");
  set("sound", "The acoustics of the sewer are vast and varied.");

  set("noise_time", 5);
  set("noise_output", ({
    "A slow drip is heard from somewhere nearby.\n",
    "Sudden screaming is heard from someplace deep within the sewers.\n",
    "A gutteral roar is heard from somewhere nearby.\n",
    "The area suddenly shakes slightly, like an explosion went off far " +
      "above.\n",
  }));
  
  set("exits", ([
    "north" : NS_ROOM "tunnel_e1_upper3",
    "south" : NS_ROOM "tunnel_e1_upper5",
    "east"  : NS_ROOM "tunnel_conn_2oclock",
  ]));

  ::create();
}

void init () {
  add_action("_activate", "activate");
  add_action("_answer",   "answer");

  ::init();
}

int _activate (string str) {

  if (member_array(str, ({ "terminal", "the terminal" }) ) == -1) {
    notify_fail("Activate what?\n");
    return 0;
  }

  /* Only normal difficulty players can access this area.  Remember to
     add a notify_fail here if the player's eval is below 2500 (or relative) 
     && the difficulty is not set to normal, when implemented later. */

  if(state_tactivate_entered) {
    notify_fail("The terminal has already been activated.\n");
    return 0;
  }

  uu=random(1000); vv=random(1000);
  ii=(uu<vv?random(uu):random(vv));
  bb=uu+vv-ii+random(1000); nn=-(uu+vv-ii-bb);
  rq=random(2);
  switch(rq) {
    case 0 :
      t_question = "There are "+bb+" scum in the Nepos sewers. " +
                   uu+" of them like guns, "+vv+" of them like drugs. " +
                   nn+" like neither. How many like guns AND drugs? " +
                   "Type 'answer <number>' to answer.";
      break;
    case 1 : 
      t_question = "There are "+bb+" scum in the Nepos sewers. " +
                   uu+" of them like guns, "+vv+" of them like drugs. " +
                   ii+" like both. How many like neither guns nor drugs? " +
                   "Type 'answer <number>' to answer.";
      break;
  }

  state_tactivate_entered = 1;
  state_tactivate_exited  = 0;

  message("general", wrap("\
You activate the terminal which flickers briefly and displays a message."), TP);

  t_desc = "The terminal displays the message: " + t_question;
  set_item_descs();

  message("general", sprintf("%s activates the terminal in the corner " + 
             "of the room\n", TPCNAME), TPENV, TP);

  return 1;
}

int _answer (string str) {

  if(state_tactivate_exited) {
    notify_fail("Answer what? The terminal isn't even on.\n");
    return 0;
  }

  nn_toStr = "" + nn;
  ii_toStr = "" + ii;

  if(rq) {
    if (member_array(str, ({ nn_toStr }) ) == -1) {
      notify_fail("The terminal buzzes and briefly displays that that was " + 
                  "an incorrect answer.\n");
      return 0;
    }
  }
  else {
    if (member_array(str, ({ ii_toStr }) ) == -1) {
      notify_fail("The terminal buzzes and briefly displays that that was " + 
                  "an incorrect answer.\n");
      return 0;
    }
  }

  message("general", wrap("\
You punch in the answer, which the terminal happily accepts, then promptly 
turns itself off."), TP);
  message("general", sprintf("%s types something in the terminal, which " + 
    "opens up a passage. They quickly slip through.\n", TPCNAME), TPENV, TP);

  message("general", wrap("\
The wall opens up, and you slip through to the other side before it closes 
up again."), TP);

  message("general", sprintf("%s enters, the wall quickly closing " +
    "behind them.\n", TPCNAME), NS_ROOM "ss_primary_part1");
  TP->move(NS_ROOM "ss_primary_part1");
  TP->force_me("look -b");

  t_desc                   = "While the terminal looks like it has been " +
                           "frequently used, the screen only presents a " + 
                  "simple prompt. Perhaps one could 'activate terminal'?";
  set_item_descs();

  state_tactivate_entered = 0;
  state_tactivate_exited  = 1; 

  return 1;
}

void event_monster_killed() {

  event_monsters--;
}

void set_ss_event() {

  if(event_monsters > 30)
    return; 

  rand_monsters = random(10);
  for (count_monsters = 0; count_monsters < rand_monsters; count_monsters++) {
    object event_monster = clone_object(NS_MON "sstech");
    event_monster->set_spawned_by_event();
    event_monster->move(TO);
    event_monsters++;
  }
  rand_monsters = random(10);
  for (count_monsters = 0; count_monsters < rand_monsters; count_monsters++) {
    object event_monster = clone_object(NS_MON "ssdoctor");
    event_monster->set_spawned_by_event();
    event_monster->move(TO);
    event_monsters++;
  }
  rand_monsters = random(10);
  for (count_monsters = 0; count_monsters < rand_monsters; count_monsters++) {
    object event_monster = clone_object(NS_MON "ssprof");
    event_monster->set_spawned_by_event();
    event_monster->move(TO);
    event_monsters++;
  }

  set("short", "%^HIB%^a Nepos sewer tunnel :: upper-east section%^NOR%^");
}

void end_ss_event() {

  set("short", "a Nepos sewer tunnel :: upper-east section");
}

