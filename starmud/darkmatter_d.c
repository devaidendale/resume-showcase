#include SILENUS_H

inherit DAEMON;

void darkmatter_d_save();
void darkmatter_d_restore();

mapping DEPOSIT_BOX = ([ ]);
mapping DARKMATTER_BANK = ([ ]);
mapping DARKMATTER_LOG = ([ ]);  /* Effectively 'reputation'. */

string query_deposit_box(object player) {
  string player_name;
  mixed *player_box;
  object query_item;
  string box_contents;
  string item_stacks;

  if(!playerp(player) || !stringp(player_name = getuid(player)))
    return 0;

  if(arrayp(player_box = DEPOSIT_BOX[player_name])) {
    box_contents = "\n";
    for(int idx = 0; idx < sizeof(player_box); idx++) {
      query_item = clone_object(player_box[idx][0]);
      player_box[idx][1] > 1 ? item_stacks = "[" + player_box[idx][1] + " of] " : item_stacks = "";
      box_contents = box_contents + "-- " + item_stacks + query_item->query("short") + "\n";
      query_item->remove();
    }
    return box_contents;
  }
  else
    return "\n-- (nothing)\n";
}

string query_darkmatter_bank(object player) {
  string player_name;
  mixed *player_bank;
  object query_item;
  string bank_contents;

  if(!playerp(player) || !stringp(player_name = getuid(player)))
    return 0;

  if(arrayp(player_bank = DARKMATTER_BANK[player_name])) {
    bank_contents = "\n";
    for(int idx = 0; idx < sizeof(player_bank); idx++) {
      query_item = clone_object(player_bank[idx][0]);
      bank_contents = bank_contents + "-- " + query_item->query("short") + "\n";
      query_item->remove();
    }
    return bank_contents;
  }
  else
    return "\n-- (nothing)\n";
}

int deposit_depositbox_item(string player_name, object item) {
  mixed *player_box;
  string item_filename;
  
  if(!stringp(player_name) || !objectp(item))
    return 0;

  item_filename = explode(file_name(item), "#")[0];

  if(arrayp(player_box = DEPOSIT_BOX[player_name])) {
    for(int idx = 0; idx < sizeof(player_box); idx++) {
      if(player_box[idx][0] == item_filename) {
        player_box[idx][1]++;
        DEPOSIT_BOX[player_name] = player_box;

        /* Prevent possible data loss from reboot: */
        if(WAR_D->query_wartime() == -1) {
          remove_call_out("darkmatter_d_save");
          call_out("darkmatter_d_save", 10);
        }
        return 1;
      }
    }
    player_box = player_box + ({ ({ item_filename, 1 }) });
  }
  else
    player_box = ({ ({ item_filename, 1 }) });

  DEPOSIT_BOX[player_name] = player_box;

  /* Prevent possible data loss from reboot: */
  if(WAR_D->query_wartime() == -1) {
    remove_call_out("darkmatter_d_save");
    call_out("darkmatter_d_save", 10);
  }

  return 1;
}

int deposit_darkmatter_item(object player, object item) {
  string player_name;
  mixed *player_bank;
  string item_filename;
  
  if(!playerp(player) || !objectp(item) || !stringp(player_name = getuid(player)))
    return 0;

  item_filename = explode(file_name(item), "#")[0];

  if(arrayp(player_bank = DARKMATTER_BANK[player_name])) {
    for(int idx = 0; idx < sizeof(player_bank); idx++) {
      if(player_bank[idx][0] == item_filename)
        return 0;
    }
    player_bank = player_bank + ({ ({ item_filename, item->query("dark_matter_withdraw_time") }) });
  }
  else
    player_bank = ({ ({ item_filename, item->query("dark_matter_withdraw_time") }) });

  DARKMATTER_BANK[player_name] = player_bank;

  /* Prevent possible data loss from reboot: */
  if(WAR_D->query_wartime() == -1) {
    remove_call_out("darkmatter_d_save");
    call_out("darkmatter_d_save", 10);
  }

  return 1;
}

int withdraw_darkmatter_item(object player, string item) {
  string player_name;
  mixed *player_bank;
  mixed *player_bank_modified;
  object withdraw_item;
  string withdraw_file;
  int withdraw_time;
  
  if(!playerp(player) || !stringp(item) || !stringp(player_name = getuid(player)))
    return 0;

  if(!arrayp(player_bank = DARKMATTER_BANK[player_name]))
    return 0;

  for(int idx = 0; idx < sizeof(player_bank); idx++) {
    withdraw_file = player_bank[idx][0];
    withdraw_item = clone_object(withdraw_file);
    if(withdraw_item->id(item)) {
      withdraw_time = player_bank[idx][1];
      if(sizeof(player_bank) > 1) {
        int flattened_idx = 0;
        player_bank_modified = allocate(sizeof(player_bank) - 1);
        for(int jdx = 0; jdx < sizeof(player_bank); jdx++) {
          if(idx == jdx)
            continue;
          player_bank_modified[flattened_idx] = ({ player_bank[jdx][0], player_bank[jdx][1] });
          flattened_idx++;
        }
      }
      break;
    }
    else
      withdraw_item->remove();
  }
  if(objectp(withdraw_item)) {

    withdraw_item->set("obtained_by", player_name);
    withdraw_item->set("no_store", 1);
    withdraw_item->set("prevent_give", 1);
    withdraw_item->set("prevent_sell", 1);
    withdraw_item->set("prevent_get", 1);
    withdraw_item->set("short", withdraw_item->query("short") + " (%^HIM%^shimmering%^NOR%^)");
    withdraw_item->set("drop_destroy", wrap("\
The " + withdraw_item->query("id")[0] + " disintegrates into nothingness as it is dropped."));

    if(!withdraw_time)
      withdraw_item->set("dark_matter_withdraw_time", time());
    else if(time() < (withdraw_time + 86400)) {
      if(withdraw_item->move(player)) {
        withdraw_item->set("dark_matter_withdraw_time", withdraw_time);
        player_bank = player_bank_modified;
        DARKMATTER_BANK[player_name] = player_bank;

        /* Prevent possible data loss from reboot: */
        if(WAR_D->query_wartime() == -1) {
          remove_call_out("darkmatter_d_save");
          call_out("darkmatter_d_save", 10);
        }
        return 2;
      }
      else {
        withdraw_item->remove();
        return -1;
      }
    }
    if(withdraw_item->move(player)) {
      withdraw_item->set("dark_matter_withdraw_time", time());
      player_bank = player_bank_modified;
      DARKMATTER_BANK[player_name] = player_bank;

      /* Prevent possible data loss from reboot: */
      if(WAR_D->query_wartime() == -1) {
        remove_call_out("darkmatter_d_save");
        call_out("darkmatter_d_save", 10);
      }
      return 1;
    }
    else {
      withdraw_item->remove();
      return -1;
    }
  }

  return 0;
}

int withdraw_depositbox_item(object player, string item) {
  string player_name;
  mixed *player_box;
  mixed *player_box_modified;
  object withdraw_item;
  string withdraw_file;
  
  if(!playerp(player) || !stringp(item) || !stringp(player_name = getuid(player)))
    return 0;

  if(!arrayp(player_box = DEPOSIT_BOX[player_name]))
    return 0;

  for(int idx = 0; idx < sizeof(player_box); idx++) {
    withdraw_file = player_box[idx][0];
    withdraw_item = clone_object(withdraw_file);
    if(withdraw_item->id(item)) {
      if(player_box[idx][1] == 1) {
        if(sizeof(player_box) > 1) {
          int flattened_idx = 0;
          player_box_modified = allocate(sizeof(player_box) - 1);
          for(int jdx = 0; jdx < sizeof(player_box); jdx++) {
            if(idx == jdx)
              continue;
            player_box_modified[flattened_idx] = ({ player_box[jdx][0], player_box[jdx][1] });
            flattened_idx++;
          }
        }
      }
      else {
        player_box_modified = player_box;
        player_box_modified[idx][1]--;
      }
      break;
    }
    else
      withdraw_item->remove();
  }
  if(objectp(withdraw_item)) {

    withdraw_item->set("obtained_by", player_name);

    if(withdraw_item->move(player)) {
      player_box = player_box_modified;
      DEPOSIT_BOX[player_name] = player_box;

      /* Prevent possible data loss from reboot: */
      if(WAR_D->query_wartime() == -1) {
        remove_call_out("darkmatter_d_save");
        call_out("darkmatter_d_save", 10);
      }
        return 1;
    }
    else {
      withdraw_item->remove();
      return -1;
    }   
  }

  return 0;
}

void log_transaction(string player, int amt) {
  if(!DARKMATTER_LOG[player])
    DARKMATTER_LOG[player] = amt;
  else
    DARKMATTER_LOG[player] = DARKMATTER_LOG[player] + amt;

  /* Prevent possible data loss from reboot: */
  if(WAR_D->query_wartime() == -1) {
    remove_call_out("darkmatter_d_save");
    call_out("darkmatter_d_save", 10);
  }
}

mapping get_darkmatter_log() {
  return DARKMATTER_LOG;
}

void create() {
  if(clonep(TO)) {
    call_out("remove", 0);
    return ::create();
  }
  darkmatter_d_restore();

  return ::create();
}

void reset() {
  int backup_reboot_time;

  if(clonep(TO))
    return ::reset();

  if (!random(20))
    cp(DARKMATTER_OBJ + "data/darkmatter_d.o", DARKMATTER_OBJ + "data/darkmatter_d.o.bak");

  /* Prevent possible data loss from reboot: */
  backup_reboot_time = WAR_D->query_wartime();
  if(backup_reboot_time < 3600) {
     if(backup_reboot_time <= 0)
       darkmatter_d_save();
     else {
       remove_call_out("darkmatter_d_save");
       call_out("darkmatter_d_save", WAR_D->query_wartime() + 90);
     }
  }
  
  darkmatter_d_save();
  return ::reset();
}

void remove() {
  if(clonep(TO))
    return ::remove();

  darkmatter_d_save();
  return ::remove();
}

void darkmatter_d_save() {
  if(clonep(TO))
    return;

  save_object(DARKMATTER_OBJ + "data/darkmatter_d.o");
}

void darkmatter_d_restore() {
  if (clonep(TO))
    return;

  restore_object(DARKMATTER_OBJ + "data/darkmatter_d.o");
}
