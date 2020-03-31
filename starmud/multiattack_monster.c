#include SILENUS_H

inherit MONSTER;

/* Defaults */
int multiattack_damage = 5;
string multiattack_type = "unarmed";
float multiattack_growth = 1.3;
string multiattack_msg = "combine their might to launch a deadly attack " +
                         "on you!";
string multiattack_omsg = "combine their might to launch a deadly attack " +
                          "on %s!";
string multiattack_def = "defense";
int multiattack_def_coeff = 10;

void configure_multiattack(int dam, string type, float growth, string msg, 
                           string omsg, string def, int coeff);
void set_multiattack_cooldown();
void process_multiattack_cooldown();
void multiattack();

void create() {
  set("multiattack_cooldown", 0);
  set("multiattack_cooldown_epoch", 0);

  ::create();
}

/* Called in a multiattack monster (that inherits this instead of MONSTER)'s 
   create(). */
void configure_multiattack(int dam, string type, float growth, string msg, 
                           string omsg, string def, int coeff) {
  multiattack_damage = dam;
  multiattack_type = type;
  multiattack_growth = growth;
  multiattack_msg = msg;
  multiattack_omsg = omsg;
  multiattack_def = def;
  multiattack_def_coeff = coeff;
}

/* Called when the 'master' multiattack monster invokes a multiattack for the 
   entire group. */
void set_multiattack_cooldown() {
  set("multiattack_cooldown", 1);
  set("multiattack_cooldown_epoch", time());
}

/* Called to query if a multiattack has cooled down and thus can be used 
   again for the group. */
void process_multiattack_cooldown() {
  if(time() > query("multiattack_cooldown_epoch") + 5) {
    set("multiattack_cooldown", 0);
  }
}

void multiattack() {
  int dam, multiattack_size;
  object ob, *other_monsters;
  string msg, omsg;

  if (!playerp(ob = query_attacker()) || !present(ob, TOENV))
    return;

  process_multiattack_cooldown();
  if(!query("multiattack_cooldown")) {
    other_monsters = visible_inventory(TOENV, TO, 1);
    multiattack_size = 0;
    for(int idx = 0; idx < sizeof(other_monsters); idx++) {
      if(explode(file_name(other_monsters[idx]), "#")[0] == 
         explode(file_name(TO), "#")[0]) {

        if(!other_monsters[idx]->query_attacker()) 
          continue; /* Do not count if not in combat. */

        other_monsters[idx]->set_multiattack_cooldown();
        multiattack_size++;
      }
    }
    if(multiattack_size >= 2) { 
      set_multiattack_cooldown();

      dam = multiattack_damage * (pow(multiattack_size, multiattack_growth));
      /* Default growth rate (5*n^1.3):
         2 = 12   3 = 20   4 = 30
         5 = 40   6 = 51   7 = 62
         8 = 74   9 = 86  10 = 99 ... */

      dam -= (1 + ob->query_skill(multiattack_def)) / multiattack_def_coeff;
      if(dam < 10)
        dam = 10;
      if(!random(10))
        ob->improve_skill(multiattack_def, 1);
     
      msg = "The %^HIR%^" + consolidate(multiattack_size, query_name()) + 
            "%^NOR%^ currently fighting " + multiattack_msg;
      omsg = "The " + consolidate(multiattack_size, query_name()) + 
             " currently fighting " + multiattack_omsg;

      message("general", wrap(msg), ob);
      message("general", wrap(sprintf(omsg, ob->query_cap_name())), TOENV, ob);

      ob->receive_damage(dam, multiattack_type, "overall");
    }
  }
}
