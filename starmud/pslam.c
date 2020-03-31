#include <lib.h>
#include <defines.h>
#include <combat_events.h>

inherit COMMAND;

void print_combat_msg(int damage, object victim);

int filter_monster( object ob ) {
  return living(ob) && !userp(ob) && !ob->query_is_helper();
}

int filter_player( object ob ) {
  if( living(ob) && userp(ob) && interactive(ob) && !wizardp(ob) &&
      !testp(ob) && this_player() != ob )
    return 1;
  return 0;
}

int do_slm(object victim, mixed *secondary_victims) {
  int damage, damage_reduction, secondary_hits, secondary_damage, endurance_complement;
  float mult;
  string logging;

  damage = prandom(
             TP->query_skill("critical")         +
             TP->query_skill("legerdemain")      +
             TP->query_skill("intimidation") * 2 +
             TP->query_skill("unarmed")      * 2 +
             TP->query_stat("strength")      * 5
           );

  damage = damage / 2;

  damage_reduction = prandom(
                       victim->query_skill("defense")   / 2 + 
                       victim->query_skill("toughness") / 2 + 
                       victim->query_stat("agi")
                     );

  damage = MAX(damage - damage_reduction, 20);

  if (monsterp(TP))
    damage = damage / 2;
  else {
    mult = 1 + (random(TP->query_skill("critical")) - random(2000)) / 100.0;
    mult = MIN(mult, 2.0);

    if (mult >= 2.0) {
      message("combat-special-hit",
        "%^BOLD%^<< EXTREME HARDCORE CRITICAL POWERSLAM! >>%^NOR%^\n", TP);
      message("combat-special-hit",
        "%^BOLD%^>>> RECEIVED CRITICAL POWERSLAM! <<<%^NOR%^\n", victim);
      TP->improve_skill("critical", 4);
      victim->improve_skill("sixth sense", 4);
      damage = damage * mult;
    }
    else if (mult >= 1.5) {
      message("combat-special-hit",
        "%^BOLD%^<< HARDCORE CRITICAL POWERSLAM! >>%^NOR%^\n", TP);
      message("combat-special-hit",
        "%^BOLD%^>>> RECEIVED CRITICAL POWERSLAM! <<<%^NOR%^\n", victim);
      TP->improve_skill("critical", 2);
      victim->improve_skill("sixth sense", 2);
      damage = damage * mult;
    }
    else if (mult > 1.0) {
      message("combat-special-hit",
        "%^BOLD%^<< CRITICAL POWERSLAM! >>%^NOR%^\n", TP);
      message("combat-special-hit",
        "%^BOLD%^>>> RECEIVED CRITICAL POWERSLAM! <<<%^NOR%^\n", victim);
      TP->improve_skill("critical", 1);
      victim->improve_skill("sixth sense", 1);
      damage = damage * mult;
    }  
  }

  print_combat_msg(damage, victim);

  logging = sprintf("PSLAM: %d damage by %s" + (mult > 1 ? " (crit " + mult + "x)" : ""), damage, CNAME(TP));

  victim->receive_damage(damage, "blunt", "overall");

  if(sizeof(secondary_victims) > 0) {
    secondary_hits = to_int(MIN(sqrt(MAX(0.0, 
                                         to_float(TP->query_skill("intimidation") + 
                                         TP->query_stat("strength") - 10 - 
                                         (10 * sizeof(secondary_victims)))
                                        )), sizeof(secondary_victims)));

    secondary_damage = to_int(MIN(sqrt(to_float(TP->query_skill("intimidation"))) * (TP->query_stat("strength") / 4), damage));

    if(mult > 1)
      secondary_damage = secondary_damage * ((1 + mult) / 2);

    /* The unusual endurance logic is so inorganics, endurance hooks 
       and endurance-modified races are roughly captured without wasting CPU 
       and memory querying it all. */
    endurance_complement = TP->query_endurance() - (secondary_hits * 100) - (secondary_damage / 10);
    if(TP->endurance((-secondary_hits * 100) - (secondary_damage / 10)) < 0) {
      message("general", 
        wrap("You were too exhausted to complete the move and dealt less damage!"), TP);
      secondary_damage = MAX(1, secondary_damage - ABS(endurance_complement));
    }

    logging += sprintf(", %d/%d other targets took %d damage", secondary_hits, sizeof(secondary_victims), secondary_damage);

    if(secondary_damage > 20) {
      TP->improve_skill("intimidation", 1 + ((random(2 * TP->query_stat("int")) / 100) * (secondary_hits / 10)));

      for(int idx = 0; idx < secondary_hits; idx++) {
        message("combat-special-hit", wrap("The sheer brutality of " + CNAME(TP) + 
          "'s powerslam reverberates around " + OBJ(TP) + " with deadly " + 
          "force, hitting you in the process!"), secondary_victims[idx]);
        secondary_victims[idx]->receive_damage(secondary_damage, "blunt", "overall");
      }

      message("combat-special-hit", "%^HIM%^" + wrap("The sheer brutality of your " + 
        "powerslam reverberates around you with deadly force, hitting " + 
        (secondary_hits > 1 ? consolidate(secondary_hits, "of your other assailant") :
        "another of your assailants") + "!") + "%^NOR%^", TP);
    }
  }
  else
    logging += ", no other targets";

  if(!random(20))
    log_db("player", "cmd", getuid(TP), logging);

  return secondary_hits;
}

int main(string str) {
  object victim, *obs;
  mixed *secondary_victims;
  string fail_reason;
  int chance, spd_bonus, secondary_hits;

  if(!str || !strlen(str))
    victim = TP->query_attacker();
  else
    victim = present(TP->resolve_nickname(str), TPENV);

  if(TP->query("slm_delay"))
    return notify_fail("You must rest before attempting another powerslam.\n");

  if(TP->query_combat_event(PARALYZED))
    return notify_fail("You are paralyzed and have no control of your body.\n");

  if(!victim && str == "monster") {
    obs = all_inventory( TPENV );
    obs = filter( obs, "filter_monster", this_object() );
    if ( sizeof(obs) ) victim = obs[0];
  }

  if(!victim && str == "player") {
    obs = all_inventory( TPENV );
    obs = filter( obs, "filter_player", this_object() );
    if( sizeof( obs ) ) victim = obs[0];
  }

  if( !victim || !visible(victim, TP) )
    return notify_fail("You don't see them here.\n");

  if((fail_reason = prevent_attack_reason(TP, victim)))
    return notify_fail(fail_reason);

  chance = (TP->query_skill("unarmed") + TP->query_skill("critical"))
           - (victim->query_skill("defense") + random(victim->query_skill("sixth sense")));

  if(chance > 92) chance = 92;
  if(chance < 5) chance = 5;

  /*
  message("combat-info", sprintf(
    "POWERSLAM: hit chance = %d \n", chance),
        filter(all_inventory(environment(TP)), (: wizardp :)));
  */

  if(random(100) >= chance) {
    message("combat-special-miss", wrap(sprintf("\
You attempt to powerslam %s, but fail miserably.",
      CNAME(victim))), TP);
    message("combat-special-miss", wrap(sprintf("\
You get lucky and evade %s's powerslam.",
      TPCNAME)), victim);
    message("combat-special-miss", wrap(sprintf("\
%s tries to grab %s for a powerslam but %s dodges out the way.",
      TPCNAME, CNAME(victim), SUBJ(victim))), TPENV, ({ TP, victim }) );

    TP->improve_skill("unarmed", random(2));
  } else {
    TP->improve_skill("unarmed", 2 + (random(TP->query_stat("int")) / 25));
    victim->cmd_cost(40, "You are still finding your feet.\n");

    secondary_victims = filter(INV(TPENV), 
                        (:  living($1) && 
                            $1 != $2   &&
                            $1 != $3   &&
                            (arrayp($1->dump_will_attack()) && sizeof($1->dump_will_attack() & ({ $3 })) == 1)
                        :), victim, TP);

    secondary_hits = do_slm(victim, secondary_victims);
  }

  spd_bonus = MAX(50, (TP->query_stat("spd") + TP->query_stat("con") + TP->query_skill("intimidation")) / 40);
  TP->cmd_cost(80 + secondary_hits - spd_bonus, "You are still recovering from your powerslam.\n");
  TP->set_temp("slm_delay", 1, (4 + random(4)));
  TP->attack(victim);
  return 1;
}

void print_combat_msg( int damage, object victim ) {
  string vic;
  string str1, str2, str3;

  vic = (string) victim->query_cap_name();

  switch( to_int(damage) ) {
  case 0..199:
    str1 = "You scoop up "+vic+" and try to break them over your body!\n";
    str2 = ""+TPCNAME+" attempts to break your back!\n";
    str3 = ""+TPCNAME+" grins evilly as they slam "+vic+" to the ground!\n";
  break;
  case 200..499:
    str1 = "You lift up "+vic+" and drive their spine into the ground!\n";
    str2 = ""+TPCNAME+" lifts you up and smashes you into the ground!\n";
    str3 = ""+TPCNAME+" lifts up "+vic+" and smashes them into the ground!\n";
  break;
  case 500..899:
    str1 = "You crush "+vic+"'s body under the weight of your powerslam!\n";
    str2 = ""+TPCNAME+" cackles gleefully as they crush your body!\n";
    str3 = ""+TPCNAME+" cackles gleefully as "+vic+" crashes into the ground!\n";
  break;
  case 900..1499:
    str1 = "You bury "+vic+"'s body into the ground with a mighty powerslam!\n";
    str2 = ""+TPCNAME+" buries you with a mighty powerslam!\n";
    str3 = ""+TPCNAME+" buries "+vic+" with a mighty powerslam!\n";
  break;
  case 1500..1999:
    str1 = "You brutally powerslam "+vic+" with no remorse!\n";
    str2 = ""+TPCNAME+" brutally powerslams you with no remorse!\n";
    str3 = ""+TPCNAME+" brutally powerslams "+vic+" with no remorse!\n";
  break;
  default:
    str1 = "You utterly annihilate "+vic+" with an eye-watering powerslam!\n";
    str2 = ""+TPCNAME+" utterly annihilates you with an eye-watering powerslam!\n";
    str3 = ""+TPCNAME+" utterly annihilates "+vic+" with an eye-watering powerslam!\n";
  break;
  }

  message("combat-special-hit", "%^HIB%^" + str1 + "%^NOR%^", TP);
  message("combat-special-hit", str2, victim);
  message("combat-special-hit", str3, TPENV, ({ TP, victim }));
  if( WAR_D->query_war() && this_object()->query_warring() ) {
    call_other("/d/war/spec_room/screen", "???");
    tell_room("/d/war/spec_room/screen", wrap("[Monitor] "+ str3));
  }

  return;
}

int help() {
  printf("
Usage: pslam <target>

Powerslam is a powerful slam trained only by the most fearsome of unarmed 
fighters: the Thugs. This technique has been perfected to cause fierce 
damage and with the right skills can cause critical damage to a foe's body 
and potentially nearby assailants.\n");
  return 1;
}
