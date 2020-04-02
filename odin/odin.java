// Odin.java

// <header comment omitted for privacy>

import java.util.*;
import java.io.*;

import java.time.*;
import java.text.SimpleDateFormat;
import java.text.ParseException;

import sx.blah.discord.api.IDiscordClient;
import sx.blah.discord.api.ClientBuilder;
import sx.blah.discord.api.events.EventDispatcher;
import sx.blah.discord.api.events.IListener;
import sx.blah.discord.handle.impl.events.guild.channel.message.MessageReceivedEvent;
import sx.blah.discord.handle.obj.IChannel;
import sx.blah.discord.handle.obj.IMessage;
import sx.blah.discord.handle.obj.IUser;
import sx.blah.discord.util.DiscordException;
import sx.blah.discord.util.MessageBuilder;
import sx.blah.discord.util.MissingPermissionsException;
import sx.blah.discord.util.RateLimitException;

public class Odin implements IListener<MessageReceivedEvent>
{
    public static enum PlayerClass
    {
        DRUID_BALANCE("balance druid"),
        DRUID_FERAL("feral druid"),
        DRUID_RESTORATION("restoration druid"),

        HUNTER_BEASTMASTER("beastmaster hunter"),
        HUNTER_MARKSMAN("marksman hunter"),
        HUNTER_SURVIVAL("survival hunter"),

        MAGE_ARCANE("arcane mage"),
        MAGE_FIRE("fire mage"),
        MAGE_FROST("frost mage"),

        PALADIN_HOLY("holy paladin"),
        PALADIN_PROTECTION("protection paladin"),
        PALADIN_RETRIBUTION("retribution paladin"),

        PRIEST_HOLY("holy priest"),
        PRIEST_SHADOW("shadow priest"),

        ROGUE_ASSASSINATION("assassination rogue"),
        ROGUE_OUTLAW("outlaw rogue"),
        ROGUE_SUBTLETY("subtlety rogue"),

        WARLOCK_AFFLICTION("affliction warlock"),
        WARLOCK_DEMONOLOGY("demonology warlock"),
        WARLOCK_DESTRUCTION("destruction warlock"),

        WARRIOR_ARMS("arms warrior"),
        WARRIOR_FURY("fury warrior"),
        WARRIOR_PROTECTION("protection warrior");

        private String name;

        PlayerClass(String name) { this.name = name; }
        public String toString() { return name; }
    }

    public static enum Boss
    {
        LUCIFRON("Lucifron"),
        MAGMADAR("Magmadar"),
        GEHENNAS("Gehennas"),
        GARR("Garr"),
        GEDDON("Baron Geddon"),
        SHAZZRAH("Shazzrah"),
        SULFURON("Sulfuron Harbinger"),
        GOLEMAGG("Golemagg"),
        MAJORDOMO("Majordomo Executus"),
        RAGNAROS("Ragnaros"),

        ONYXIA("Onyxia");

        private String name;

        Boss(String name) { this.name = name; }
        public String toString() { return name; }
    }

    ArrayList<Player> raiders;
    ArrayList<String> absentList;
    boolean raidInProgress;

    // Discord API-specific variables.
    IDiscordClient client;
    EventDispatcher dispatcher;

    // Used to anchor secondary channels.
    IChannel shittalk;
    IChannel officerLoot;

    // Used for when a message needs to also send a message to the secondary channel.
    String specialOutput;

    public Odin()
    {
        raiders = new ArrayList<Player>();
        absentList = new ArrayList<String>();
        raidInProgress = false;

        shittalk = null;
        officerLoot = null;
        specialOutput = null;
    }

    public void run()
    {
        // Read raiders file.
        readRaiders();

        client = createClient(/* Omitted for security */, true);
        dispatcher = client.getDispatcher();

        dispatcher.registerListener(this);
    }

    public void readRaiders()
    {
        try
        {
            Scanner readFile = new Scanner(new File("data/raiders.dat"));
            while(readFile.hasNextLine())
            {
                String tokens[] = readFile.nextLine().split(";");

                String raiderName = tokens[0];
                String raiderSpec = tokens[1];
                String raiderClass = tokens[2];
                int raiderMajorStanding = Integer.parseInt(tokens[3]);
                int raiderMinorStanding = Integer.parseInt(tokens[4]);
                double raiderRaidsAttended = Double.parseDouble(tokens[5]);
                int raiderTotalRaids = Integer.parseInt(tokens[6]);

                String getClassEnum = raiderClass.toUpperCase() + "_" + raiderSpec.toUpperCase();
                PlayerClass raiderClassE = PlayerClass.valueOf(getClassEnum);

                raiders.add(new Player(raiderName, raiderClassE, raiderMajorStanding, raiderMinorStanding, raiderRaidsAttended, raiderTotalRaids));
            }
            readFile.close();
        }
        catch(IOException e)
        {
            System.out.println(e);
            System.exit(0);
        }
    }

    public String lateJoin(String content)
    {
        if(!raidInProgress)
            return "There is no raid in progress for a late player to even join.";

        if(content == null)
            return "You didn't supply the name of the late player.";

        String latePlayerName = null;

        for(int idx = 0; idx < absentList.size(); idx++)
        {
            if(content.equals(absentList.get(idx)))
            {
                latePlayerName = absentList.get(idx);
                absentList.remove(idx);
                break;
            }
        }

        if(latePlayerName == null)
            return "That player either wasn't marked as late or doesn't actually exist.";
        else
        {
            Player latePlayer = null;
            for(int idx = 0; idx < raiders.size(); idx++)
            {
                if(raiders.get(idx).getName().equals(latePlayerName))
                {
                    latePlayer = raiders.get(idx);
                    latePlayer.lateJoin();
                    latePlayer.modifyMinorStandingBy(1);
                    latePlayer.modifyMajorStandingBy(1);
                    /* Note: No distinction between prog and non-prog.  To do this, prog will have to be a global. */
                    break;
                }
            }
            if(latePlayer == null)
                return "An unexplainable error occurred.";
            else
                return "Raider " + latePlayerName + " joined late, thus lowering their absent penalty and counting as half-attended and will gain standing only from this point on in the current raid.";
        }
    }

    public String leftEarly(String content)
    {
      if(!raidInProgress)
          return "There is no raid in progress for a player to even leave early.";

      if(content == null)
          return "You didn't supply the name of the player who left early.";

      String leftEarlyPlayerName = null;

      for(int idx = 0; idx < absentList.size(); idx++)
      {
          if(content.equals(absentList.get(idx)))
          {
              leftEarlyPlayerName = absentList.get(idx);
              break;
          }
      }

      if(leftEarlyPlayerName != null)
          return "That player was marked as absent.  How can an absent player leave early?";
      else
      {
          leftEarlyPlayerName = content;
          Player leftEarlyPlayer = null;
          for(int idx = 0; idx < raiders.size(); idx++)
          {
              if(raiders.get(idx).getName().equals(leftEarlyPlayerName))
              {
                  leftEarlyPlayer = raiders.get(idx);
                  leftEarlyPlayer.leftEarly();
                  leftEarlyPlayer.modifyMinorStandingBy(-1);
                  leftEarlyPlayer.modifyMajorStandingBy(-1);
                  absentList.add(leftEarlyPlayerName);
                  /* Note: No distinction between prog and non-prog.  To do this, prog will have to be a global. */
                  break;
              }
          }
          if(leftEarlyPlayer == null)
              return "That player doesn't seem to exist.";
          else
              return "Raider " + leftEarlyPlayerName + " left early, thus slightly lowering their standing and counting as being half-attended and will no longer gain any standing from this point on in the current raid.";
      }
    }

    public String endRaid()
    {
        if(!raidInProgress)
            return "There is no raid to conclude, because no raid was started.";

        // Write new raider data to file.
        try
        {
            PrintWriter pw = new PrintWriter(new FileOutputStream(new File("data/raiders.dat"), false));

            for(int i = 0; i < raiders.size(); i++)
            {
                Player currentRaider = raiders.get(i);

                String raiderName = currentRaider.getName();
                String raiderType = currentRaider.playerClass.toString();
                String raiderTypeSpecClass[] = raiderType.split(" ");
                int raiderMinor = currentRaider.getMinor();
                int raiderMajor = currentRaider.getMajor();
                double raiderRaidsAttended = currentRaider.getRaidsAttended();
                int raiderTotalRaids = currentRaider.getTotalRaids();

                pw.println(raiderName + ";" + raiderTypeSpecClass[0] + ";" + raiderTypeSpecClass[1] + ";" + raiderMajor + ";" + raiderMinor + ";" + raiderRaidsAttended + ";" + raiderTotalRaids);
            }

            pw.close();
        }
        catch(IOException e)
        {
            System.out.println(e);
            System.exit(0);
        }

        raidInProgress = false;
        absentList = new ArrayList<String>();
        return "Concluded the raid successfully.";
    }

    public String newRaid(String content)
    {
        // This command is used once at the start of a raid when everyone present is accounted for.
        // It will deduct standings from absent raiders, and increase the standings of all those present.
        // The absent list is then held in memory for item distribution, because present raiders need to go up in standing when items are handed out.
        // This is needed because it would create a non-uniform distribution.  Also, this means that the program can't terminate during a raid... since I cbf writing a raid object.

        // Ideally: program is terminated at the end of the raid, so there's no absent list.
        // Sub-optimal: endRaid removes the absent list, 'concluding' the raid.
        // Consistency check: if newRaid hasn't been called, any item distribution will spit out an error.
        boolean prog = false;

        if(officerLoot == null)
            return "Officer channel anchor point was not set.  Write something in it first, then try again.";

        if(raidInProgress)
            return "There already is a raid in progress.  Conclude that raid first.";

        String returnString = "";
        if(content == null)
        {
            returnString = "New raid started with no absentees.";
        }
        else
        {

            String getTokens[] = content.split(" ");

            if(getTokens[0].equals("prog"))
            {
                returnString = "New PROG raid started with absentees: ";
                prog = true;
            }
            else
            {
                returnString = "New raid started with absentees: ";
            }

            for(int i = 0; i < getTokens.length; i++)
            {
                // First token is 'prog' on a prog raid, so we have to skip it.
                if(i == 0 && prog == true)
                    continue;

                char absentType = getTokens[i].charAt(0);
                String raiderName = getTokens[i].substring(1, getTokens[i].length());

                Player foundRaider = null;
                for(int j = 0; j < raiders.size(); j++)
                {
                    if(raiderName.equals(raiders.get(j).getName()))
                    {
                        foundRaider = raiders.get(j);

                        // Poor attendance equals more harsher penalties for not attending raids.
                        int attendanceModifier = 1;
                        if(foundRaider.getAttendancePercent() < 80)
                            attendanceModifier++;

                        if (absentType == '~')
                        {
                            // '~' prefix: late.
                            returnString = returnString + raiderName + " (late) ";
                            // This seems high because they will be added points for being in the raid, still.
                            foundRaider.modifyMinorStandingBy(-4);
                            foundRaider.modifyMajorStandingBy(-4);
                        }
                        else
                        {
                            if(absentType == '-')
                            {
                                // '-' prefix: no reason.
                                returnString = returnString + raiderName + " (no reason) ";
                                foundRaider.modifyMinorStandingBy(-6 * attendanceModifier);
                                foundRaider.modifyMajorStandingBy(-6 * attendanceModifier);
                            }
                            else if(absentType == '+')
                            {
                                // '+' prefix: reason provided.
                                returnString = returnString + raiderName + " (reason provided) ";
                                foundRaider.modifyMinorStandingBy(-4 * attendanceModifier);
                                foundRaider.modifyMajorStandingBy(-4 * attendanceModifier);
                            }
                            else if(absentType == '#')
                            {
                                // '#' prefix: exempt.
                                returnString = returnString + raiderName + " (exempt) ";
                            }
                            else
                            {
                                // Incorrectly formatted absent type; break.
                                absentList = new ArrayList<String>();
                                return "You've formatted that incorrectly.  No changes were made; try again.";
                            }
                            absentList.add(foundRaider.getName());
                        }

                        break;

                    }
                }
                if(foundRaider == null)
                {
                    // Raider doesn't exist; break.
                    absentList = new ArrayList<String>();
                    return "One or more of the raiders in the absentee list you provided doesn't exist, or the formatting was bad.  No changes were made; try again.";
                }

            }
        }

        // Add x to all the non-absent raiders' standings.  This puts the total 'benefit' to being present to +x+y over someone who isn't there without a reason.
        returnString = returnString + "\n\nRaider attendance:\n";
        raidInProgress = true;
        for(int i = 0; i < raiders.size(); i++)
        {
            Player thisRaider = raiders.get(i);
            boolean isAbsent = false;
            for(int j = 0; j < absentList.size(); j++)
            {
                if(thisRaider.getName().equals(absentList.get(j)))
                {
                    isAbsent = true;
                    thisRaider.missedRaid();
                    break;
                }
            }
            if(!isAbsent)
            {
                thisRaider.attendedRaid();
                if(prog)
                {
                    thisRaider.modifyMinorStandingBy(20);
                }
                else
                {
                    thisRaider.modifyMinorStandingBy(1);
                }
                if(prog)
                {
                    thisRaider.modifyMajorStandingBy(20);
                }
                else
                {
                    thisRaider.modifyMajorStandingBy(1);
                }
            }
        }

        // Sort the raider list by attendance.
        for(int i = raiders.size() - 1; i >= 0; i--)
        {
            for(int j = 0; j < i; j++)
            {
                if(raiders.get(j).getAttendancePercent() < raiders.get(j+1).getAttendancePercent())
                {
                    // Swap:
                    Player temp = raiders.get(j+1);
                    raiders.set((j+1), raiders.get(j));
                    raiders.set(j, temp);
                }
            }
        }
        // Output new sorted list.
        returnString = returnString + showRaiders();

        return returnString;
    }

    public String nextMinor(String content)
    {
        if(!raidInProgress)
            return "This command will only function when there's an actual raid in progress.";

        if(content == null)
            return "No raiders were supplied for that command.";
        String getTokens[] = content.split(" ");
        if(getTokens.length == 1)
            return "You do realise if there's one raider in the list they're not competing against anyone, right?";

        int highest = -7;
        Player thisRaider = null;
        Player highestRaider = null;

        ArrayList<Player> sortedByAttendance = new ArrayList<Player>();
        ArrayList<Player> sortedByPoints = new ArrayList<Player>();

        for(int i = 0; i < getTokens.length; i++)
        {
            for(int j = 0; j < raiders.size(); j++)
            {
                if(getTokens[i].equals(raiders.get(j).getName()))
                {
                    thisRaider = raiders.get(j);

                    sortedByAttendance.add(thisRaider);
                    sortedByPoints.add(thisRaider);

                    if(thisRaider.getMinor() > highest)
                    {
                        highest = thisRaider.getMinor();
                        highestRaider = raiders.get(j);
                    }
                    break;
                }
            }
            if(thisRaider == null)
                return "One of the raiders in the list provided doesn't actually exist.";
            thisRaider = null;
        }
        if(highestRaider != null)
        {
            // Check if the winner has <80% attendance.
            boolean attendanceFlag = false;
            if(highestRaider.getAttendancePercent() < 80)
            {
                // Check if anyone else in the list has >=80% attendance.
                for(int i = 0; i < sortedByAttendance.size(); i++)
                {
                    if(sortedByAttendance.get(i).getAttendancePercent() >= 80)
                    {
                        attendanceFlag = true;
                        break;
                    }
                }
            }
            if(attendanceFlag)
            {
                // Since a non-winner in the list has good attendance, sort the two lists as per their name.
                for(int i = sortedByAttendance.size() - 1; i >= 0; i--)
                {
                    for(int j = 0; j < i; j++)
                    {
                        if(sortedByAttendance.get(j).getAttendancePercent() < sortedByAttendance.get(j+1).getAttendancePercent())
                        {
                            // Swap:
                            Player temp = sortedByAttendance.get(j+1);

                            sortedByAttendance.set((j+1), sortedByAttendance.get(j));
                            sortedByAttendance.set(j, temp);
                        }
                    }
                }

                for(int i = 0; i < sortedByPoints.size(); i++)
                {
                    for(int j = 0; j < i; j++)
                    {
                        if(sortedByPoints.get(j).getMinor() < sortedByPoints.get(j+1).getMinor())
                        {
                            // Swap:
                            Player temp = sortedByPoints.get(j+1);

                            sortedByPoints.set((j+1), sortedByPoints.get(j));
                            sortedByPoints.set(j, temp);
                        }
                    }
                }

                // Construct the two strings that will be relayed to two channels.
                specialOutput = "This is the sorted-by-minor standings of the list just entered in the loot channel:\n";
                for(int i = 0; i < sortedByPoints.size(); i++)
                {
                    specialOutput = specialOutput + "  " + sortedByPoints.get(i).getName() + " (" + sortedByPoints.get(i).getMinor() + " minor standing) (" + sortedByPoints.get(i).getAttendancePercent() + "% attendance)\n";
                }
                String normalOutput = "Raider(s) attendance in that list is a factor.  Officers will review a list ordered by their standing.  That list is ordered by attendance here for non-officers:\n";
                for(int i = 0; i < sortedByAttendance.size(); i++)
                {
                    normalOutput = normalOutput + "  " + sortedByAttendance.get(i).getName() + "(" + sortedByAttendance.get(i).getAttendancePercent() + "% attendance)\n";
                }

                return normalOutput;
            }
            // Otherwise, the winner has >=80% attendance, so it just goes by points.
            else
            {
                return "The next raider in that list to receive minor loot is: " + highestRaider.getName();
            }
        }
        else
        {
            return "Something happened which shouldn't be possible.";
        }

    }

    public String nextMajor(String content)
    {
        if(!raidInProgress)
            return "This command will only function when there's an actual raid in progress.";

        if(content == null)
            return "No raiders were supplied for that command.";
        String getTokens[] = content.split(" ");
        if(getTokens.length == 1)
            return "You do realise if there's one raider in the list they're not competing against anyone, right?";

        int highest = -7;
        Player thisRaider = null;
        Player highestRaider = null;

        ArrayList<Player> sortedByAttendance = new ArrayList<Player>();
        ArrayList<Player> sortedByPoints = new ArrayList<Player>();

        for(int i = 0; i < getTokens.length; i++)
        {
            for(int j = 0; j < raiders.size(); j++)
            {
                if(getTokens[i].equals(raiders.get(j).getName()))
                {
                    thisRaider = raiders.get(j);

                    sortedByAttendance.add(thisRaider);
                    sortedByPoints.add(thisRaider);

                    if(thisRaider.getMajor() > highest)
                    {
                        highest = thisRaider.getMajor();
                        highestRaider = raiders.get(j);
                    }
                    break;
                }
            }
            if(thisRaider == null)
                return "One of the raiders in the list provided doesn't actually exist.";
            thisRaider = null;
        }
        if(highestRaider != null)
        {
            // Check if the winner has <80% attendance.
            boolean attendanceFlag = false;
            if(highestRaider.getAttendancePercent() < 80)
            {
                // Check if anyone else in the list has >=80% attendance.
                for(int i = 0; i < sortedByAttendance.size(); i++)
                {
                    if(sortedByAttendance.get(i).getAttendancePercent() >= 80)
                    {
                        attendanceFlag = true;
                        break;
                    }
                }
            }
            if(attendanceFlag)
            {
                // Since a non-winner in the list has good attendance, sort the two lists as per their name.
                for(int i = sortedByAttendance.size() - 1; i >= 0; i--)
                {
                    for(int j = 0; j < i; j++)
                    {
                        if(sortedByAttendance.get(j).getAttendancePercent() < sortedByAttendance.get(j+1).getAttendancePercent())
                        {
                            // Swap:
                            Player temp = sortedByAttendance.get(j+1);

                            sortedByAttendance.set((j+1), sortedByAttendance.get(j));
                            sortedByAttendance.set(j, temp);
                        }
                    }
                }

                for(int i = 0; i < sortedByPoints.size(); i++)
                {
                    for(int j = 0; j < i; j++)
                    {
                        if(sortedByPoints.get(j).getMajor() < sortedByPoints.get(j+1).getMajor())
                        {
                            // Swap:
                            Player temp = sortedByPoints.get(j+1);

                            sortedByPoints.set((j+1), sortedByPoints.get(j));
                            sortedByPoints.set(j, temp);
                        }
                    }
                }

                // Construct the two strings that will be relayed to two channels.
                specialOutput = "This is the sorted-by-major standings of the list just entered in the loot channel:\n";
                for(int i = 0; i < sortedByPoints.size(); i++)
                {
                    specialOutput = specialOutput + "  " + sortedByPoints.get(i).getName() + " (" + sortedByPoints.get(i).getMajor() + " major standing) (" + sortedByPoints.get(i).getAttendancePercent() + "% attendance)\n";
                }
                String normalOutput = "Raider(s) attendance in that list is a factor.  Officers will review a list ordered by their standing.  That list is ordered by attendance here for non-officers:\n";
                for(int i = 0; i < sortedByAttendance.size(); i++)
                {
                    normalOutput = normalOutput + "  " + sortedByAttendance.get(i).getName() + " (" + sortedByAttendance.get(i).getAttendancePercent() + "% attendance)\n";
                }

                return normalOutput;
            }
            // Otherwise, the winner has >=80% attendance, so it just goes by points.
            else
            {
                return "The next raider in that list to receive major loot is: " + highestRaider.getName();
            }
        }
        else
        {
            return "Something happened which shouldn't be possible.";
        }

    }

    public String lootMinor(String content)
    {
        if(!raidInProgress)
            return "This command will only function when there's an actual raid in progress.";

        Player theRaider = null;
        for(int i = 0; i < raiders.size(); i++)
        {
            if(raiders.get(i).getName().equals(content))
            {
                theRaider = raiders.get(i);
                break;
            }
        }

        if(theRaider == null)
            return "That raider doesn't actually exist.  No changes were made.";

        for(int i = 0; i < absentList.size(); i++)
        {
            if(theRaider.getName().equals(absentList.get(i)))
                return "That raider was marked as absent for this raid.  If they joined late, use '!latejoin <name>'.";
        }

        theRaider.lootMinor();

        // Add +1 minor standing to all other present raiders.
        for(int i = 0; i < raiders.size(); i++)
        {
            Player thisOtherRaider = raiders.get(i);

            // Skip self.
            if(thisOtherRaider.getName().equals(theRaider.getName()))
                continue;

            boolean isAbsent = false;

            for(int j = 0; j < absentList.size(); j++)
            {
                if(thisOtherRaider.getName().equals(absentList.get(j)))
                {
                    isAbsent = true;
                    break;
                }
            }
            if(!isAbsent)
            {
                thisOtherRaider.modifyMinorStandingBy(1);
            }
        }

        return "Raider " + theRaider.getName() + " received minor loot successfully.";
    }

    public String lootMajor(String content)
    {
        if(!raidInProgress)
            return "This command will only function when there's an actual raid in progress.";

        Player theRaider = null;
        for(int i = 0; i < raiders.size(); i++)
        {
            if(raiders.get(i).getName().equals(content))
            {
                theRaider = raiders.get(i);
                break;
            }
        }

        if(theRaider == null)
            return "That raider doesn't actually exist.  No changes were made.";

        for(int i = 0; i < absentList.size(); i++)
        {
            if(theRaider.getName().equals(absentList.get(i)))
                return "That raider was marked as absent for this raid.  If they joined late, use '!latejoin <name>'.";
        }

        theRaider.lootMajor();

        // Add +1 major standing to all other present raiders.
        for(int i = 0; i < raiders.size(); i++)
        {
            Player thisOtherRaider = raiders.get(i);

            // Skip self.
            if(thisOtherRaider.getName().equals(theRaider.getName()))
                continue;

            boolean isAbsent = false;

            for(int j = 0; j < absentList.size(); j++)
            {
                if(thisOtherRaider.getName().equals(absentList.get(j)))
                {
                    isAbsent = true;
                    break;
                }
            }
            if(!isAbsent)
            {
                thisOtherRaider.modifyMajorStandingBy(1);
            }
        }

        return "Raider " + theRaider.getName() + " received major loot successfully.";
    }

    public String addRaider(String content)
    {
        if(content == null)
            return "Syntax: '!addraider <name> <spec> <class>'.  Eg. '!addraider Dadhole beastmaster hunter'.";
        String getTokens[] = content.split(" ", 3);
        if(getTokens.length != 3)
            return "Syntax: '!addraider <name> <spec> <class>'.  Eg. '!addraider Dadhole beastmaster hunter'.";
        if(raidInProgress)
            return "You can't add a raider when there's already a raid in progress.  They can wait.";

        String raiderName = getTokens[0].toLowerCase();
        String raiderSpec = getTokens[1].toLowerCase();
        String raiderClass = getTokens[2].toLowerCase();

        // Check for duplicate names.
        for(int i = 0; i < raiders.size(); i++)
        {
            if(raiderName.equals(raiders.get(i).getName()))
                return "That raider already exists, you moron.";
        }

        // Check that spec and class actually exist.
        String getClassEnum = raiderClass.toUpperCase() + "_" + raiderSpec.toUpperCase();
        PlayerClass raiderClassE = null;
        try
        {
            raiderClassE = PlayerClass.valueOf(getClassEnum);
        }
        catch(IllegalArgumentException e)
        {
            return "Whatever in Hel a " + raiderSpec + " " + raiderClass + " is, it is not worthy to be one of my raiders.";
        }

        try
        {
            PrintWriter pw = new PrintWriter(new FileOutputStream(new File("data/raiders.dat"), true));
            pw.println(raiderName + ";" + raiderSpec + ";" + raiderClass + ";0;0;0;0");
            pw.close();

            // Also want to add this entry to the wantlist:
            pw = new PrintWriter(new FileOutputStream(new File("data/raiders_wantlist.dat"), true));
            pw.println(raiderName + ";0;0;0;0;0;0;0;0;0;0;0");
            pw.close();
        }
        catch(IOException e)
        {
            System.out.println(e);
            System.exit(0);
        }
        raiders.add(new Player(raiderName, raiderClassE, 0, 0, 0, 0));

        return "Added " + raiderName + ", the " + raiderClassE + ".";
    }

    public String showRaiders()
    {
        int horiz_count = 0;
        int left_complement = 0;
        int right_complement = 0;
        String returnString = "```\n  ";

        for(int i = 0; i < raiders.size(); i++)
        {
            //returnString = returnString + raiders.get(i) + "\n";

            if((++horiz_count) == 4)
            {
                returnString += "\n  ";
                horiz_count = 1;
            }
            returnString = returnString + raiders.get(i).getName();

            left_complement = 12 - raiders.get(i).getName().length();
            right_complement = 3 - String.valueOf(raiders.get(i).getAttendancePercent()).length();
            for(int j = 0; j < 2 + left_complement + right_complement; j++)
            {
                returnString = returnString + ".";
            }
            returnString = returnString + raiders.get(i).getAttendancePercent() + "%   ";
        }
        if(returnString.equals("```\n  "))
            returnString = "I have no raiders!  For shame!";
        else
            returnString = returnString + "\n```";

        return returnString;
    }

    public String whoWants(String content)
    {
        if(content == null)
            return "For what boss?";

        int bossName_idx = 0;

        content = content.toLowerCase();

        if(content.equals("lucifron"))
            bossName_idx = 1;
        else if(content.equals("magmadar"))
            bossName_idx = 2;
        else if(content.equals("gehennas"))
            bossName_idx = 3;
        else if(content.equals("garr"))
            bossName_idx = 4;
        else if(content.equals("geddon"))
            bossName_idx = 5;
        else if(content.equals("shazzrah"))
            bossName_idx = 6;
        else if(content.equals("sulfuron"))
            bossName_idx = 7;
        else if(content.equals("golemagg"))
            bossName_idx = 8;
        else if(content.equals("majordomo"))
            bossName_idx = 9;
        else if(content.equals("ragnaros"))
            bossName_idx = 10;
        else if(content.equals("onyxia"))
            bossName_idx = 11;

        else if(content.equals("xarcos"))
            return "Who doesn't?";

        if(bossName_idx == 0)
            return "There is no boss with that name.  (lucifron|magmadar|gehennas|garr|geddon|shazzrah|sulfuron|golemagg|majordomo|ragnaros|onyxia)";
        else
        {
            // Read the file and the boss index into string array.
            String wantListKey[] = new String[raiders.size()];
            int wantListValue[] = new int[raiders.size()];
            int currIdx = 0;

            try
            {
                Scanner readFile = new Scanner(new File("data/raiders_wantlist.dat"));
                while(readFile.hasNextLine())
                {
                    String tokens[] = readFile.nextLine().split(";");

                    String raiderName = tokens[0];
                    int ilvlsForThisBoss = Integer.parseInt(tokens[bossName_idx]);

                    wantListKey[currIdx] = raiderName;
                    wantListValue[currIdx] = ilvlsForThisBoss;

                    currIdx++;
                }
                readFile.close();
            }
            catch(IOException e)
            {
                System.out.println(e);
                System.exit(0);
            }

            // Order by descending (simple bubble sort since there'll never be more than 30 entries).
            if(raiders.size() > 1)
            {
                for(int i = raiders.size() - 1; i >= 0; i--)
                {
                    for(int j = 0; j < i; j++)
                    {
                        if(wantListValue[j] > wantListValue[j+1])
                        {
                            // Swap:
                            String bubble_tempKey = wantListKey[j+1];
                            int bubble_tempValue = wantListValue[j+1];

                            wantListKey[j+1] = wantListKey[j];
                            wantListValue[j+1] = wantListValue[j];
                            wantListKey[j] = bubble_tempKey;
                            wantListValue[j] = bubble_tempValue;
                        }
                    }
                }
            }
            // Construct final string.
            String returnString = "";
            for(int i = raiders.size() - 1; i >= 0; i--)
            {
                // Skip 0 values.
                if(wantListValue[i] == 0)
                    continue;

                returnString = returnString + wantListKey[i] + " wants " + wantListValue[i] + " ilvls worth of upgrades.\n";
            }
            if(returnString.equals(""))
                returnString = "No one wants anything from that boss.";

            return returnString;
        }
    }

    public String resolveDiscordName(String authorName)
    {
        if(authorName.equals(/* Omitted */))
            authorName = /* Omitted */;
        else if(authorName.equals(/* Omitted */))
            authorName = /* Omitted */;

        else
            authorName = "NOT_RAIDER_OR_NOT_RESOLVED";

        return authorName;
    }

    public String whatDoIWant(String authorName)
    {
        boolean isRaider = false;
        for(int i = 0; i < raiders.size(); i++)
        {
            if(authorName.equals(raiders.get(i).getName()))
            {
                isRaider = true;
                break;
            }
        }

        // Resolve Discord->/* Omitted */ names:
        String authorName_temp = authorName;
        if(isRaider == false)
            authorName = resolveDiscordName(authorName);
        if(!authorName.equals("NOT_RAIDER_OR_NOT_RESOLVED"))
            isRaider = true;
        if(isRaider)
        {
            String result = "Uhh... I dunno.";
            try
            {
                Scanner readFile = new Scanner(new File("data/raiders_wantlist.dat"));
                while(readFile.hasNextLine())
                {
                    String tokens[] = readFile.nextLine().split(";");
                    if(tokens[0].equals(authorName))
                    {
                        result = authorName + " wants:\n";
                        for(int i = 0; i < 11; i++)
                        {
                            String thisboss = "";
                            switch(i)
                            {
                                case 0:  thisboss = "lucifron"; break;
                                case 1:  thisboss = "magmadar"; break;
                                case 2:  thisboss = "gehennas"; break;
                                case 3:  thisboss = "garr"; break;
                                case 4:  thisboss = "geddon"; break;
                                case 5:  thisboss = "shazzrah"; break;
                                case 6:  thisboss = "sulfuron"; break;
                                case 7:  thisboss = "golemagg"; break;
                                case 8:  thisboss = "majordomo"; break;
                                case 9:  thisboss = "ragnaros"; break;
                                case 10: thisboss = "onyxia"; break;
                            }
                            if(tokens[i+1].equals("0"))
                                result = result + "nothing from " + thisboss + ".\n";
                            else
                                result = result + tokens[i+1] + " ilvls of upgrades from " + thisboss + ".\n";
                        }
                        break;
                    }
                }
                readFile.close();
            }
            catch(IOException e)
            {
                System.out.println(e);
                System.exit(0);
            }
            return result;
        }
        else
        {
            return "You aren't a raider, or your discord name hasn't been linked to your wow name.  Your discord name is '" + authorName_temp + "'.";
        }
    }

    public String iWant(String authorName, String content)
    {
        boolean isRaider = false;
        for(int i = 0; i < raiders.size(); i++)
        {
            if(authorName.equals(raiders.get(i).getName()))
            {
                isRaider = true;
                break;
            }
        }

        // Resolve Discord->WoW names:
        String authorName_temp = authorName;
        if(isRaider == false)
            authorName = resolveDiscordName(authorName);
        if(!authorName.equals("NOT_RAIDER_OR_NOT_RESOLVED"))
            isRaider = true;


        if(content == null)
            return "You formatted that wrong.  'iwant <boss> <ilvl sum>'.";

        if(isRaider)
        {
            String tokens[] = content.split(" ");
            if(tokens.length != 2)
                return "You formatted that wrong.  'iwant <boss> <ilvl sum>'.";
            String bossName = tokens[0].toLowerCase();

            int bossName_idx = 0;
            if(bossName.equals("lucifron"))
                bossName_idx = 1;
            else if(bossName.equals("magmadar"))
                bossName_idx = 2;
            else if(bossName.equals("gehennas"))
                bossName_idx = 3;
            else if(bossName.equals("garr"))
                bossName_idx = 4;
            else if(bossName.equals("geddon"))
                bossName_idx = 5;
            else if(bossName.equals("shazzrah"))
                bossName_idx = 6;
            else if(bossName.equals("sulfuron"))
                bossName_idx = 7;
            else if(bossName.equals("golemagg"))
                bossName_idx = 8;
            else if(bossName.equals("majordomo"))
                bossName_idx = 9;
            else if(bossName.equals("ragnaros"))
                bossName_idx = 10;
            else if(bossName.equals("onyxia"))
                bossName_idx = 11;

            if(bossName_idx == 0)
                return "That boss doesn't exist.  (lucifron|magmadar|gehennas|garr|geddon|shazzrah|sulfuron|golemagg|majordomo|ragnaros|onyxia)";

            int ilvlSum = 0;
            try
            {
                ilvlSum = Integer.parseInt(tokens[1]);
            }
            catch(NumberFormatException e)
            {
                return "Does that look like a number to you?";
            }
            if(ilvlSum < 0)
                return "Nice try.  I'll put you in the 'retarded' list instead.";

            // Read file, write new file with this new data.  Hopefully the thread is locked when this occurs.
            String wantListKey[] = new String[raiders.size()];
            int wantListValues[][] = new int[raiders.size()][11];
            int currIdx = 0;

            try
            {
                Scanner readFile = new Scanner(new File("data/raiders_wantlist.dat"));
                while(readFile.hasNextLine())
                {
                    String tokens2[] = readFile.nextLine().split(";");

                    wantListKey[currIdx] = tokens2[0];
                    for(int i = 0; i < 11; i++)
                    {
                        wantListValues[currIdx][i] = Integer.parseInt(tokens2[i+1]);
                    }

                    currIdx++;
                }
                readFile.close();
            }
            catch(IOException e)
            {
                System.out.println(e);
                System.exit(0);
            }

            // Overwrite file with the new data.
            try
            {
                PrintWriter pw = new PrintWriter(new FileOutputStream(new File("data/raiders_wantlist.dat"), false));
                for(int i = 0; i < raiders.size(); i++)
                {
                    // Check if this is the raider that made the changes to their entry.
                    if(authorName.equals(wantListKey[i]))
                    {
                        String updatedEntry = authorName;
                        for(int j = 1; j <= 11; j++)
                        {
                            if(j == bossName_idx)
                                updatedEntry = updatedEntry + ";" + ilvlSum;
                            else
                                updatedEntry = updatedEntry + ";" + wantListValues[i][j-1];
                        }
                        pw.println(updatedEntry);
                    }
                    else
                    {
                        pw.println(wantListKey[i] + ";" + wantListValues[i][0] + ";" + wantListValues[i][1] + ";" + wantListValues[i][2] + ";" + wantListValues[i][3] + ";" + wantListValues[i][4] + ";" + wantListValues[i][5] + ";" + wantListValues[i][6] + ";" + wantListValues[i][7] + ";" + wantListValues[i][8] + ";" + wantListValues[i][9] + ";" + wantListValues[i][10]);
                    }
                }
                pw.close();
            }
            catch(IOException e)
            {
                System.out.println(e);
                System.exit(0);
            }

            return "Raider " + authorName + " now wants " + ilvlSum + " ilvls worth of upgrades from " + bossName + ".";
        }
        else
        {
            return "You aren't a raider, or your discord name hasn't been linked to your wow name.  Your discord name is '" + authorName_temp + "'.";
        }

    }

    public String onBoss(String content)
    {
        LocalDate localDate = LocalDate.now();
        LocalTime localTime = LocalTime.now();

        String[] h_m_s = localTime.toString().split(":");
        int hour = Integer.parseInt(h_m_s[0]);
        int minute = Integer.parseInt(h_m_s[1]);
        String ampm = "";
        if(hour > 12)
            { ampm = "PM"; hour -= 12; }
        else
            ampm = "AM";

        String[] yearmonthday = localDate.toString().split("-");
        int year = Integer.parseInt(yearmonthday[0]);
        int month = Integer.parseInt(yearmonthday[1]);
        int day = Integer.parseInt(yearmonthday[2]);

        String dayOfWeek = "";
        try
        {
            String dateString = String.format("%d-%d-%d", year, month, day);
            Date date = new SimpleDateFormat("yyyy-M-d").parse(dateString);
            dayOfWeek = new SimpleDateFormat("EEEE", Locale.ENGLISH).format(date);
        }
        catch(ParseException e)
        {
            e.printStackTrace();
        }

        String minute_padding = "";
        if(minute < 10)
            minute_padding = "0";

        return "Now on boss **" + content + "** at **" + dayOfWeek + ", " + hour + ":" + minute_padding + minute + " " + ampm + "**";
    }

    public boolean checkOfficer(String officerName)
    {
        if(officerName.equals(/* Omitted */) || officerName.equals(/* Omitted */) ||
           officerName.equals(/* Omitted */) || officerName.equals(/* Omitted */) ||
           officerName.equals(/* Omitted */) || officerName.equals(/* Omitted */))
            return true;
        else
            return false;
    }

    // This string is only called once, ever, for the announce message for this tier.
    public String tempinit()
    {
        return "This loot system controls loot distribution and some raid administration.  Note the following:\n" +
        "1. All raiders are on two lists to receive loot: the major upgrade list, and the minor upgrade list.\n" +
        "2. If a raider receives an item, they're sent to the back of that list.  Both lists are independent from each other; if you receive a major upgrade, your minor upgrade position stays the same, and vice versa.\n" +
        "3. Major upgrades typically require the item to be either BiS or an actual major upgrade.  Don't select this if it isn't!  Officers and class leaders have discretion here for various reasons; talk to them about it if you need more information.\n" +
        "4. Offspec items, or unwanted items, are not on any list.  You may /roll for them without affecting any standings.\n" +
        "5. Absent raiders will incur a penalty to their standing, but the penalty is smaller if they provided a reason.  Late raiders will incur a very small penalty, as long as they're no later than the time of the first boss kill.  Raiders who are frequently missing raids will incur significant loss to their standing.\n" +
        "6. Attendance matters a lot with your standing, and your attendance percent is visible to everyone.  Try to achieve at least an 80% attendance rate and you should not have any problems.  You *might* still lose an item to a raider behind you in standing if your attendance is poor.  This is at the discretion of the officers, and will be transparent (to a degree to avoid collusion) in the loot channel.\n" +
        "7. Your standing is masked to prevent loot collusion, and only officers can see it *if necessary*.  Don't ask an officer to reveal your standing.\n" +
        "8. Progression (not farmed) raids are where you earn most standing.  There are currently no progression raids, but this will be applicable in the future.\n" +
        "9. If you're sitting out and on standby for any reason, you *will* still earn standing as though you were present.";
    }

    public void handle(MessageReceivedEvent event)
    {
        IMessage message = event.getMessage();
        IChannel channel = message.getChannel();
        IUser author = event.getAuthor();

        String authorName = author.getName().toLowerCase();
        String channelName = channel.getName();
        String content = message.getContent();

        // Anchor point for officer-loot channel.
        if(officerLoot == null && channelName.equals("officer-loot"))
        {
            officerLoot = channel;
        }
        // This is the secondary output string for when commands need to send to officer-loot:
        specialOutput = null;

        // Shit-talking module (remote controlled messages).
        if(shittalk == null && channelName.equals("general"))
        {
            shittalk = channel;
        }
        if(shittalk != null && channelName.equals("rhee"))
        {
            try
            {
                new MessageBuilder(client).withChannel(shittalk).withContent(content).build();
            }
            catch (RateLimitException e)
            {
                System.err.print("Sending messages too quickly!");
                e.printStackTrace();
            }
            catch (DiscordException e)
            {
                System.err.print(e.getErrorMessage());
                e.printStackTrace();
            }
            catch (MissingPermissionsException e)
            {
                System.err.print("Missing permissions for channel!");
                e.printStackTrace();
            }
            try
            {
                new MessageBuilder(client).withChannel(channel).withContent("Sent successfully.").build();
            }
            catch (RateLimitException e)
            {
                System.err.print("Sending messages too quickly!");
                e.printStackTrace();
            }
            catch (DiscordException e)
            {
                System.err.print(e.getErrorMessage());
                e.printStackTrace();
            }
            catch (MissingPermissionsException e)
            {
                System.err.print("Missing permissions for channel!");
                e.printStackTrace();
            }
        }

        if(channelName.equals("loot-channel") || channelName.equals("loot-wanted"))
        {
            // Parse the call character, and the command itself from the parameters.
            String getCommand[] = content.split(" ", 2);
            char botcall = getCommand[0].charAt(0);
            String command = getCommand[0].substring(1, getCommand[0].length());

            if(getCommand.length > 1)
                content = getCommand[1];
            else
                content = null;

            // Execute command only if it was in the correct channel name and the botcall token was used.
            if(botcall == '!')
            {
                String output = "";

                // loot-wanted commands:
                if(channelName.equals("loot-wanted"))
                {
                    switch(command)
                    {
                        case "whowants":
                            if(checkOfficer(authorName))
                                output = whoWants(content);
                            else
                                output = "Only officers may use this command.";
                            break;
                        case "iwant":
                            output = iWant(authorName, content);
                            break;
                        case "whatdoiwant":
                            output = whatDoIWant(authorName);
                            break;
                        default:
                            output = "Invalid command.  The commands for loot-wanted are 'iwant <boss> <ilvl sum>', 'whatdoiwant', and 'whowants <boss>'.";
                            break;
                    }
                }

                // loot commands:
                if(channelName.equals("loot-channel"))
                {
                    switch(command)
                    {
                        case "addraider":
                            if(checkOfficer(authorName))
                                output = addRaider(content);
                            else
                                output = "Only officers may use this command.";
                            break;
                        case "showraiders":
                            output = showRaiders();
                            break;
                        case "latejoin":
                            if(checkOfficer(authorName))
                                output = lateJoin(content);
                            else
                                output = "Only officers may use this command.";
                            break;
                        case "leftearly":
                            if(checkOfficer(authorName))
                                output = leftEarly(content);
                            else
                                output = "Only officers may use this command.";
                            break;
                        case "newraid":
                            if(checkOfficer(authorName))
                                output = newRaid(content);
                            else
                                output = "Only officers may use this command.";
                            break;
                        case "endraid":
                            if(checkOfficer(authorName))
                                output = endRaid();
                            else
                                output = "Only officers may use this command.";
                            break;
                        case "nextminor":
                            if(checkOfficer(authorName))
                                output = nextMinor(content);
                            else
                                output = "Only officers may use this command.";
                            break;
                        case "nextmajor":
                            if(checkOfficer(authorName))
                                output = nextMajor(content);
                            else
                                output = "Only officers may use this command.";
                            break;
                        case "lootminor":
                            if(checkOfficer(authorName))
                                output = lootMinor(content);
                            else
                                output = "Only officers may use this command.";
                            break;
                        case "lootmajor":
                            if(checkOfficer(authorName))
                                output = lootMajor(content);
                            else
                                output = "Only officers may use this command.";
                            break;
                        case "on":
                            if(checkOfficer(authorName))
                                output = onBoss(content);
                            else
                                output = "No.";
                            break;

                        case "init":
                            if(checkOfficer(authorName))
                                output = tempinit();
                            else
                                output = "Invalid command.";
                            break;

                        default:
                            output = "Invalid command.";
                            break;
                    }
                }

                try
                {
                    new MessageBuilder(client).withChannel(channel).withContent(output).build();
                }
                catch (RateLimitException e)
                {
                    System.err.print("Sending messages too quickly!");
                    e.printStackTrace();
                }
                catch (DiscordException e)
                {
                    System.err.print(e.getErrorMessage());
                    e.printStackTrace();
                }
                catch (MissingPermissionsException e)
                {
                    System.err.print("Missing permissions for channel!");
                    e.printStackTrace();
                }

                // For the cases where a secondary output needs to be produced:
                if(specialOutput != null)
                {
                    try
                    {
                        new MessageBuilder(client).withChannel(officerLoot).withContent(specialOutput).build();
                    }
                    catch (RateLimitException e)
                    {
                        System.err.print("Sending messages too quickly!");
                        e.printStackTrace();
                    }
                    catch (DiscordException e)
                    {
                        System.err.print(e.getErrorMessage());
                        e.printStackTrace();
                    }
                    catch (MissingPermissionsException e)
                    {
                        System.err.print("Missing permissions for channel!");
                        e.printStackTrace();
                    }
                }

            }
        }
    }

    public static void main(String[] args)
    {
        Odin odin = new Odin();
        odin.run();
    }

    public static IDiscordClient createClient(String token, boolean login)
    {
        ClientBuilder clientBuilder = new ClientBuilder();
        clientBuilder.withToken(token);
        try
        {
            if(login)
            {
                return clientBuilder.login();
            }
            else
            {
                return clientBuilder.build();
            }
        }
        catch(DiscordException e)
        {
            e.printStackTrace();
            return null;
        }
    }

}
