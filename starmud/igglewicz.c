#include SILENUS_H

#define DARKLIST_TYPE 0
#define DARKLIST_DESC 1
#define DARKLIST_COST 2

#define DARKLIST_PHASER_DESC "\
The dimensional phaser will upgrade your dark matter containment anomaly, \n\
granting it the ability to weaken the barriers between dimensions in areas \n\
where those barriers area thinner than usual (and thus susceptible to such \n\
an action)."

#define DARKLIST_DEPOSIT_DESC "\
This service allows the 'deposit' of a dark matter-infused item into N3C's \n\
dark matter storage facility. The deposit cost is refunded if the item has \n\
previously been withdrawn. In other words, it only costs dark matter if it \n\
is a 'fresh' item. You have the following items currently in containment: {BANK_STR}"

#define DARKLIST_WITHDRAW_DESC "\
This service allows the 'withdraw' of a dark matter-infused item from N3C's \n\
dark matter storage facility. Withdrawing an item gives it a 24-hour free \n\
period, where subsequent withdrawals will refund the dark matter cost. After \n\
24 hours of real-time, it will reset the cycle and cost dark matter to \n\
withdraw again. Withdrawn items are bound to you and cannot be traded. You \n\
have the following items currently in containment: {BANK_STR}"

inherit MONSTER;

mapping darkmatter_list = ([
  "a mentorvator"                      : ({ "consumable", DARKMATTER_OBJ +     "mentorvator",  3 }),
  "a deathovator"                      : ({ "consumable", DARKMATTER_OBJ +     "deathovator",  5 }),
  "a virtue signaller"                 : ({ "consumable", DARKMATTER_OBJ + "virtuesignaller",  3 }),
  "a dimensional phaser"               : ({    "upgrade",               DARKLIST_PHASER_DESC, 10 }),
  "the %^HIW%^deposit%^NOR%^ service"  : ({    "service",              DARKLIST_DEPOSIT_DESC,  3 }),
  "the %^HIW%^withdraw%^NOR%^ service" : ({    "service",             DARKLIST_WITHDRAW_DESC,  3 }),
]);

varargs mixed id(string str) {
  if (str == "monster")
    return 0;
  if (str == "shopkeeper")
    return 1;
  return ::id(str);
}

int query_is_shopkeeper() {
  return 1;
}

int shop_willing(object ob);
int _darkmatter_deposit(string str);
int _darkmatter_withdraw(string str);
int do_transaction(int amt, object ob, string item);
void do_upgrade(string str, object ob);
string process_list(mixed *list, mixed *vals);

void create() {
  set_name("Chief Astrophysicist Igglewicz");
  set("id", ({
    "chief astrophysicist igglewicz", "astrophysicist igglewicz",
    "igglewicz", "lead scientist", "scientist", "chief"
  }));
  set("short", "Igglewicz, the N3C chief astrophysicist");
  set("dropped_short", "The lead scientist stands by the desk, consulting various staff.");
  set("long", wrap("\
Decorated with scientific accolades, Igglewicz was one of the first to 
successfully transmit matter to and from nearby dimensions, many years before 
any other scientific arm found the means to do so. It is speculated that the 
corporations of Nepos used espionage on the Consortium's primary astrophysics 
research lab, of which she was a lead researcher. Now one of the prime movers 
of N3C, she is tasked with vengeance to both rid Nepos of the corporations' 
taint, and to continue her original research started years ago into the 
dimensions beyond. She stands tall and well-built, even for exalted human 
standards, having access to the most advanced cybernetic and biotech augments 
in the known universe."));

  set_difficulty(prandom(4000));
  set_race("human", "exalted");
  set("weight", 400);
  set("gender", "female");

  set_talkfile(NEP_MON + "igglewicz_speech.dat");

  set("prevent_attack", "@do_banish");
  set("no_scare", 1);
  set("see_stealth", 1);
  set("no_summon", 1);
  set("no_auto_kill", 1);
  set("busy_talking", 0);

  set("chat_chance", 8);
  set("chat_output", ({
    "@_chat_output1", "@_chat_output2", "@_chat_output3", "@_chat_output4"
  }));

  ::create();
}

string do_banish() {
  if(!TP || !objectp(TP))
    return "\n";
  remove_call_out("do_banish2");
  call_out("do_banish2", 2, TP);
  return wrap(CNAME(TO) + " \
swiftly hits a button by her wrist and suddenly you feel a strange pulling 
sensation beneath you.");
  return "\n";
}
void do_banish2(object ob) {
  if(!ob || !objectp(ob))
    return;
  if(!present(ob, TOENV))
    return;
  message("general", wrap("\
The floor beneath you shifts and folds into its own space, forming a vortex in 
space-time. You promptly fall into it and emerge elsewhere."), ob);
  ob->move(NEP_ROOM + "n3c_threshold");
  ob->force_me("look -b");
  call_out("do_banish3", 2, ob);
}
void do_banish3(object ob) {
  object smiley_ob;
  if(!ob || !objectp(ob))
    return;
  if(present(ob, TOENV)) {
    force_me("say Please don't do that again, " + CNAME(ob) + ".");
  }
  else {
    if(smiley_ob = present(NEP_MON + "smiley_terminal", find_object_or_load(NEP_ROOM + "n3c_threshold")))
      smiley_ob->got_kicked_out(ob);
  }
}

void tell_killer_off(object killer) {
  call_out("tell_killer_off2", 2, killer); 
}
void tell_killer_off2(object killer) {
  if(playerp(killer) && present(killer, TOENV) && !query("busy_talking"))
    force_me( ({ "say Please stop killing our soldiers, " + CNAME(killer) + ".", 
                 "say You know " + CNAME(killer) + ", we need those guys on our side.",
                 "say " + CNAME(killer) + ", save your aggression for the real enemy." })[random(3)] );
}

int shop_willing(object ob) {
  if (!visible(ob, TO)) {
    force_me("say I don't do business with people I can't see.");
    return 0;
  }

  if (ob->query_attacker()) {
    force_me(sprintf("say You are too busy to do business with me, %s.", CNAME(ob)));
    return 0;
  }

  if(!present(DARKMATTER_DEVICE, ob)) {
    force_me("say " + CNAME(ob) + ", you either destroyed your containment anomaly, or you got here via a delivery disc. " +
             "Both of these possibilities give me immeasurable disappointment in you.");
    return 0;
  }

  return 1;
}

void init() {
  add_action("_buy",    "buy");
  add_action("_sell",   "sell");  
  add_action("_list",   "list"); 
  add_action("_view",   "view");
  add_action("_haggle", "haggle");

  add_action("_darkmatter_deposit", "deposit");
  add_action("_darkmatter_withdraw", "withdraw");

  add_action("_override_talk", "talk");
  add_action("_override_talk", "ask");

  ::init();
}

int _darkmatter_deposit(string str) {
  int dm_cost;
  object deposit_item;
  string file_name_item;

  if (!stringp(str) || !strlen(str))
    return notify_fail("Deposit what? Try 'view deposit' if you need help, or 'deposit list'.\n");
  if (!shop_willing(TP))
    return 1;

  if(str == "list") {
    TP->more("Holographic text shimmers in front of you:" + DARKMATTER_D->query_darkmatter_bank(TP));
    return 1;
  }

  deposit_item = present(str, TP);
  
  if(objectp(deposit_item)) {
  file_name_item = file_name(deposit_item);
    if(file_name_item[0..16] == "/d/silenus/nepos/" && explode(file_name_item, "/")[<1][0..6] == "special" ) {
      if(!deposit_item->query("wielded") && !deposit_item->query("worn")) {
        if(deposit_item->query("dark_matter_withdraw_time") != 0)
          dm_cost = 0;
        else
          dm_cost = darkmatter_list["the %^HIW%^deposit%^NOR%^ service"][DARKLIST_COST];
        if(do_transaction(dm_cost, TP, "depositing " + str)) {
          if(DARKMATTER_D->deposit_darkmatter_item(TP, deposit_item)) {
            if(dm_cost > 0) {
              message("general", wrap(sprintf("%^WRAP%^Your %s shimmers and fades as it is deposited into the N3C dark matter storage facility. " +
                "You transfer %d quanta of dark matter from your containment anomaly to contain it there.\n",
                deposit_item->query("id")[0], dm_cost)), TP);
              message("general", wrap(sprintf("%^WRAP%^%s's containment anomaly shimmers slightly as they deposit something into N3C's dark matter storage facility.\n", 
                TPCNAME)), TPENV, TP);
            }
            else {
              message("general", wrap(sprintf("%^WRAP%^Your %s shimmers and fades as it is deposited into the N3C dark matter storage facility.\n",
                deposit_item->query("id")[0])), TP);
              message("general", wrap(sprintf("%^WRAP%^%s deposits something into N3C's dark matter storage facility.\n", 
                TPCNAME)), TPENV, TP);
            }

            deposit_item->remove();
          }
          else {
            message("general", wrap(sprintf("%^WRAP%^You try to deposit your %s into the N3C dark matter storage facility, but it fails due to having a duplicate version of it there already.\n",
                deposit_item->query("id")[0])), TP);
            do_transaction(-dm_cost, TP, "refund from depositing duplicate " + str);
          }
        }
        else {
          force_me(sprintf("say You can't afford %d quanta of dark matter for depositing, %s. " +
            "We need it to actually contain it in our storage facility, you know?", dm_cost, TPCNAME));
        }
      }
      else {
        force_me(sprintf("say You can't deposit something you have currently equipped, %s.", TPCNAME));
      }
    }
    else {
      force_me(sprintf("say That item isn't dark matter-infused, %s. " + 
        "Only the most powerful items in the universe will have this property.", TPCNAME));
    }
  }
  else {
    message("general", "You don't have that item to deposit.\n", TP);
  }

  return 1;
}

int _darkmatter_withdraw(string str) {
  int withdraw_result;
  int dm_cost;

  if (!stringp(str) || !strlen(str))
    return notify_fail("Withdraw what? Try 'view withdraw' if you need help, or 'withdraw list'.\n");
  if (!shop_willing(TP))
    return 1;

  if(str == "list") {
    TP->more("Holographic text shimmers in front of you:" + DARKMATTER_D->query_darkmatter_bank(TP));
    return 1;
  }

  dm_cost = darkmatter_list["the %^HIW%^withdraw%^NOR%^ service"][DARKLIST_COST];

  if(do_transaction(dm_cost, TP, "withdrawing " + str)) {
    withdraw_result = DARKMATTER_D->withdraw_darkmatter_item(TP, str);
    if(withdraw_result == 2) {
      do_transaction(-dm_cost, TP, "refund from successful timed withdraw of " + str);
      message("general", wrap(sprintf("%^WRAP%^The item shimmers and fades into existence as you withdraw it from the N3C dark matter storage facility.\n",
        )), TP);
      message("general", wrap(sprintf("%^WRAP%^%s withdraws something from N3C's dark matter storage facility.\n", 
        TPCNAME)), TPENV, TP);
    }
    else if(withdraw_result == 1) {
      message("general", wrap(sprintf("%^WRAP%^The item shimmers and fades into existence as you withdraw it from the N3C dark matter storage facility. " +
        "You transfer %d quanta of dark matter from your containment anomaly to stabilise it here.\n",
        dm_cost)), TP);
      message("general", wrap(sprintf("%^WRAP%^%s's containment anomaly shimmers slightly as they withdraw something from N3C's dark matter storage facility.\n", 
        TPCNAME)), TPENV, TP);
    }
    else if(withdraw_result == -1) {
      do_transaction(-dm_cost, TP, "refund from failing withdraw of over-encumbering " + str);
      message("general", "You wouldn't be able to carry that item if you withdrew it.\n", TP);
    }
    else {
      do_transaction(-dm_cost, TP, "refund from failing withdraw of non-existent " + str);
      message("general", "The storage facility does not have that item to withdraw.\n", TP);
    }
  }
  else {
    force_me(sprintf("say You can't afford %d quanta of dark matter to withdraw, %s. " +
      "We need it to actually move it from our storage facility, you know?", dm_cost, TPCNAME));
  }

  return 1;
}

int _view(string str) {
  object ob;
  string *list, *parse_str;
  mixed *vals;
  string non_object_desc;

  if (!stringp(str) || !strlen(str))
    return notify_fail("View what?\n");
  if (!shop_willing(TP))
    return notify_fail("You cannot view that object.\n");

  list = keys(darkmatter_list);
  vals = values(darkmatter_list);

  parse_str = explode(str, " ") - ({ "" });
  if(sizeof(parse_str) > 1) {    
    if(member_array(parse_str[0], ({ "a", "an", "the" })) >= 0)
      parse_str = parse_str[1..];
    if(member_array(parse_str[<1], ({ "service" })) >= 0)
      parse_str = parse_str[<2..0];
    str = parse_str[<1];
  }
  else if(str == "service") {
    return notify_fail("Which service?\n");
  }

  for(int idx = 0; idx < sizeof(list); idx++) {
    string *match_str = explode(list[idx], " ");

    if(member_array(match_str[0], ({ "a", "an", "the" })) >= 0)
      match_str = match_str[1..];

    /* Strip colour: */
    if(match_str[0][0..6] == "%^HIW%^") {
      match_str[0] = implode(explode(match_str[0], "%^HIW%^"), "");
      match_str[0] = implode(explode(match_str[0], "%^NOR%^"), "");
    }

    if(match_str[<1] == "service")
      match_str = match_str[0..<2];

    if(str == match_str[<1]) {
      if(vals[idx][DARKLIST_TYPE] == "consumable") {
        ob = find_object_or_load(vals[idx][DARKLIST_DESC]);
        message("general", sprintf("%s shows you %s:\n%s\n",
          CNAME(TO), ob->query("short"), (ob->query("long") || ob->query("closed_long"))
          ), TP);
        "/cmds/action/identify"->main(base_name(ob));
      }
      else {
        non_object_desc = replace_string(vals[idx][DARKLIST_DESC], "{BANK_STR}", DARKMATTER_D->query_darkmatter_bank(TP));
        TP->more("Holographic text shimmers in front of you:\n" + non_object_desc);
      }
      return 1;
    }
  }
  return notify_fail("That object is not available for sale.\n");
}

int _buy(string str) {
  object ob;
  string *list, *parse_str;
  mixed *vals;

  if (!stringp(str) || !strlen(str))
    return notify_fail("Buy what?\n");
  if (!shop_willing(TP))
    return 1;

  list = keys(darkmatter_list);
  vals = values(darkmatter_list);

  parse_str = explode(str, " ") - ({ "" });
  if(sizeof(parse_str) > 1) {    
    if(member_array(parse_str[0], ({ "a", "an", "the" })) >= 0)
      parse_str = parse_str[1..];
    if(member_array(parse_str[<1], ({ "service" })) >= 0)
      parse_str = parse_str[<2..0];
    str = parse_str[<1];
  }
  else if(str == "service") {
    return notify_fail("Which service?\n");
  }

  for(int idx = 0; idx < sizeof(list); idx++) {
    string *match_str = explode(list[idx], " ");

    if(member_array(match_str[0], ({ "a", "an", "the" })) >= 0)
      match_str = match_str[1..];

    /* Strip colour: */
    if(match_str[0][0..1] == "%^") 
      match_str[0] = implode(explode(match_str[0], "%^"), "");

    if(str == match_str[<1]) {
      if(vals[idx][DARKLIST_TYPE] == "consumable") {
        ob = clone_object(vals[idx][DARKLIST_DESC]);        

        if(ob->move(TP)) {
          if(do_transaction(vals[idx][DARKLIST_COST], TP, ob->query("short"))) {
            if (objectp(ENV(ob)) && living(ENV(ob)))
              ob->set("secured", 1);

            message("general", wrap(sprintf("%^WRAP%^You buy %s from %s after transferring %d quanta of dark matter from your containment anomaly.\n",
              ob->query("short"), CNAME(TO), vals[idx][DARKLIST_COST])), TP);
            message("general", wrap(sprintf("%s's containment anomaly shimmers slightly as they buy something from %s.\n", 
              TPCNAME, CNAME(TO))), TPENV, TP);
          }
          else {
            force_me(sprintf("say You can't afford %d quanta of dark matter for %s, %s. We need it to actually make it, you know?", vals[idx][DARKLIST_COST], ob->query("short"), TPCNAME));
            ob->remove();
          }
        }
        else
          force_me(sprintf("say You can't carry %s, %s.", ob->query("short"), TPCNAME));
      }
      else {
        if(vals[idx][DARKLIST_TYPE] == "service") {
          force_me("say " + CNAME(TP) + ", that is a service. You can access it via its %^HIW%^verb%^NOR%^, or %^HIW%^view%^NOR%^ it for information.");
        }
        else {
          if(do_transaction(vals[idx][DARKLIST_COST], TP, list[idx])) {
            /* Since there are really no upgrades yet, this has no buy messages to TP and TPENV. Todo. */
            do_upgrade(str, TP);
          }
          else
            force_me(sprintf("say You can't afford %d quanta of dark matter for %s, %s. We need it to actually make it, you know?", vals[idx][DARKLIST_COST], list[idx], TPCNAME));
        }
      }
      return 1;
    }
  }
  return notify_fail("That object is not available for sale.\n");
}

int do_transaction(int amt, object ob, string item) {
  object darkmatter_ob = present(DARKMATTER_DEVICE, ob);

  if(darkmatter_ob->query("dark_matter_quantity") >= amt) {
    darkmatter_ob->transact(-amt);

    /* Don't log the placeholder. Will remove when more is added. */
    if(abs(amt) != 10) {
       write_file(DARKMATTER_OBJ + "data/darkmatter_log.dat",
         ctime(time()) + ": " + CNAME(ob) + " paid " + amt + " for " + item + ".\n", 0);
       DARKMATTER_D->log_transaction(getuid(ob), amt);
    }

    return 1;
  }
  else
    return 0;
}

void do_upgrade(string str, object ob) {
  if(str == "phaser") {
    string bonus_str = (present(DARKMATTER_DEVICE, ob))->query_upgrade("dimensional phaser") ? 
      "But I see you already have it, " + CNAME(ob) + ". I hope you killed a lot of them in the process." :
      "You should investigate if you want it for yourself, " + CNAME(ob) + ".";
    force_me("say The dimensional phaser was developed by one of the R&D companies deep in Nepos City. " +
             "Since we're essentially at war with all the bureaucrats, they didn't want to share. " +
             "But we know a neutral party on the inside might have gotten their hands on it. " +
             bonus_str);
    /* Refund, since this is essentially a placeholder. */
    do_transaction(-10, ob, "refund from dimensional phaser placeholder");
  }
}

int _sell(string str) {
  if (!stringp(str) || !strlen(str))
    return notify_fail("Sell what?\n");
  if (!shop_willing(TP))
    return 1;

  force_me("say " + CNAME(TP) + ", sell us your dark matter in trade for the various tech we have %^HIW%^list%^NOR%^ed.");
  return 1;
}

int _haggle (string str) {
  if (!stringp(str) || !strlen(str))
    return notify_fail("Haggle with whom?\n");
  if (!shop_willing(TP))
    return 1;
  force_me(sprintf("say No deals, %s. This is a serious operation, not a marketplace.", TPCNAME));
  return 1;
}

string process_list(mixed *list, mixed *vals) {
  string str;

  str = "\
A holographic display appears in front of you:

 Cost      Type      Item Description
-------  ----------  --------------------------------------------------------
";

  for(int idx = 0; idx < sizeof(list); idx++) {
    str = str + sprintf("%7s  %10s  %s\n", separate_number(vals[idx][DARKLIST_COST]), 
      vals[idx][DARKLIST_TYPE], list[idx]);
  }    

  return str;
}

int _list() {
  object darkmatter_ob;
  string *list;
  string str;
  mixed *vals;

  if(!shop_willing(TP))
    return 1;

  list = keys(darkmatter_list);
  vals = values(darkmatter_list);

  str = process_list(list, vals);

  str = str + "\nYou can %^HIW%^buy%^NOR%^ and %^HIW%^view%^NOR%^ all items listed.\n";
  str = str + "Services can be accessed by typing the highlighted %^HIW%^verb%^NOR%^ as a command.\n";
  darkmatter_ob = present(DARKMATTER_DEVICE, TP);
  str = str + "\nYou have " + separate_number(darkmatter_ob->query("dark_matter_quantity")) + 
        " quanta of dark matter available in your containment anomaly.\n";

  TP->more(str);

  return 1;
}

/* The following speech functions are mostly here to reduce the retardedness 
   of how monsters talk by giving them delays and actually being linear. */
void _override_chat_output(string str) {
  if(!query("chat_chance"))
    return;

  force_me(str);
}

void _chat_output1() { 
  _override_chat_output("say N3C are always eager to have another on our side.");
}
void _chat_output2() { 
  _override_chat_output("say If you have some dark matter, you can " +
    "%^HIW%^list%^NOR%^, %^HIW%^view%^NOR%^ and %^HIW%^buy%^NOR%^ some of our " +
    "cutting edge technology in trade.");
}
void _chat_output3() { 
  _override_chat_output("say My scientists are constantly developing new " +
    "technologies with this dark matter.");
}
void _chat_output4() { 
  _override_chat_output("say We're developing some cutting edge tech here. " +
    "Talk to me if you're interested."); 
}

int _override_talk(string str) {
  string talk_target;
  string *parse_str;

  if(!stringp(str) || !strlen(str))
    return 0;

  if(strlen(str) < 4)
    return 0;

  if(str[0..2] == "to ")
    str = str[3..];

  parse_str = explode(str, " ");
  for(int idx = 1; idx < sizeof(parse_str); idx++) {
    if(parse_str[idx] == "about") {
      talk_target = parse_str[idx - 1];
      break;
    }
  }
  if(!talk_target)
    talk_target = parse_str[0];
   
  if(present(talk_target, TPENV) == TO) {
    if(query("busy_talking")) {
      message("general", wrap("\
The scientist appears to be too busy talking right now to answer you."), TP);
      return 1;
    }
    else
      return 0;
  }
  return 0;
}
void igglewicz_begin_talking() {
  set("chat_chance", 0);
  set("busy_talking", 1);
}
void igglewicz_end_talking() {
  set("chat_chance", 8);
  set("busy_talking", 0);
}
void igglewicz_delay_talking() {
  /* This is called to act as a 1-second delay in the speech file. */
}
