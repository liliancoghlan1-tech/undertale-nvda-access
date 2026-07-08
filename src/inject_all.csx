// Undertale NVDA accessibility - ALL injections in ONE pass (generated from the 17 proven inject_*.csx, build.sh order). Each wrapped in its own scope to isolate locals.

// ===== inject_nvda.csx =====
{
// Undertale accessibility - M1: speak dialogue lines via NVDA.
// Approach: watch the displayed line index (stringno) every frame and speak
// whenever it changes.  This covers ALL ways a line advances - player button
// press AND auto-advancing cutscene/intro text (which bypasses the press event).

Data.Functions.EnsureDefined("external_define", Data.Strings);
Data.Functions.EnsureDefined("external_call", Data.Strings);
Data.Functions.EnsureDefined("variable_global_exists", Data.Strings);
Data.Functions.EnsureDefined("variable_instance_exists", Data.Strings);
Data.Functions.EnsureDefined("chr", Data.Strings);
Data.Functions.EnsureDefined("string_length", Data.Strings);
Data.Functions.EnsureDefined("string_char_at", Data.Strings);
Data.Functions.EnsureDefined("string_pos", Data.Strings);

// Sanitize <srcExpr> (strip Undertale control codes, exactly mirroring the
// game's own parser in obj_base_writer Alarm_0) and speak it via NVDA.
//   ^N        = pause            -> skip 2 (^ + 1 arg)
//   \S? \E? \F? \M? \T? \*?      -> skip 3 (\ + cmd + 1 arg)
//   \z \R \G \W ... (other \X)   -> skip 2 (\ + cmd)
//   &         = newline          -> space
//   / % *     = markers/bullet   -> drop
// SpeakVia: sanitize <srcExpr> and speak it via the given bridge function
// (global.nvda_speak = interrupt-then-speak, global.nvda_speak_queue = append).
string SpeakVia(string srcExpr, string func) => @"
{
    var _raw = " + srcExpr + @";
    var _out = """";
    var _len = string_length(_raw);
    var _i = 1;
    while (_i <= _len)
    {
        var _c = string_char_at(_raw, _i);
        if (_c == ""^"") { _i += 2; continue; }
        if (_c == chr(92))
        {
            var _cmd = string_char_at(_raw, _i + 1);
            if (_cmd == ""S"" || _cmd == ""E"" || _cmd == ""F"" || _cmd == ""M"" || _cmd == ""T"" || _cmd == ""*"")
                _i += 3;
            else
                _i += 2;
            continue;
        }
        if (_c == ""&"") { _out += "" ""; _i += 1; continue; }
        if (_c == ""/"" || _c == ""%"" || _c == ""*"") { _i += 1; continue; }
        _out += _c;
        _i += 1;
    }
    if (_out != """")
    {
        // Speaker attribution: prefix the line with the character's name, but only when the
        // speaker CHANGES from the previous line (so a multi-page speech from one character
        // reads the name once, not on every page). txtsound is the per-character voice.
        var _spk = """";
        if (variable_instance_exists(id, ""txtsound""))
        {
            var _ts = txtsound;
            if (_ts == snd_txttor || _ts == snd_txttor2) _spk = ""Toriel"";
            else if (_ts == snd_floweytalk1 || _ts == snd_floweytalk2) _spk = ""Flowey"";
            else if (_ts == snd_txtsans || _ts == snd_txtsans2) _spk = ""Sans"";
            else if (_ts == snd_txtpap) _spk = ""Papyrus"";
            else if (_ts == snd_txtund || _ts == snd_txtund2 || _ts == snd_txtund3 || _ts == snd_txtund4 || _ts == snd_txtund_hyper) _spk = ""Undyne"";
            else if (_ts == snd_txtal) _spk = ""Alphys"";
            else if (_ts == snd_txtasg) _spk = ""Asgore"";
            else if (_ts == snd_txtasr || _ts == snd_txtasr2) _spk = ""Asriel"";
            else if (_ts == snd_mtt1) _spk = ""Mettaton"";
            else if (_ts == snd_tem) _spk = ""Temmie"";
            else if (_ts == snd_wngdng1) _spk = ""Gaster"";
        }
        if (_spk != """")
        {
            if (!variable_global_exists(""nvda_lastspk"") || global.nvda_lastspk != _spk)
            {
                global.nvda_lastspk = _spk;
                _out = _spk + "". "" + _out;
            }
        }
        else
        {
            global.nvda_lastspk = """";
        }
        // Priority window: while a cutscene description is protected (nvda_prio_until, measured
        // in obj_time's frame counter global.nvda_now), force this line to QUEUE behind it
        // instead of interrupting it.
        var _fn = " + func + @";
        if (variable_global_exists(""nvda_prio_until"") && variable_global_exists(""nvda_speak_queue"") && variable_global_exists(""nvda_now"") && global.nvda_now < global.nvda_prio_until)
            _fn = global.nvda_speak_queue;
        external_call(_fn, _out);
    }
}";
string SpeakCore(string srcExpr) => SpeakVia(srcExpr, "global.nvda_speak");

// charIntroGml: identify the SPEAKING character from txtsound (each major character
// has a unique typing voice, set by SCR_TEXTTYPE at box open) and, the FIRST time that
// character speaks this session, set _desc to a spoken audio-description of how they look.
// Appearance-first, no names (the game reveals those) and no story spoilers.  Detection
// by voice cleanly covers exactly the characters players confuse; generic NPCs (SND_TXT*)
// never match, so we never misidentify.  Seen-set is a session global (re-introduces on
// relaunch; persistence is a later refinement).
string charIntroGml = @"
var _ck = """";
if (variable_instance_exists(id, ""txtsound""))
{
    var _ts = txtsound;
    if (_ts == snd_txttor || _ts == snd_txttor2) _ck = ""toriel"";
    else if (_ts == snd_floweytalk1 || _ts == snd_floweytalk2) _ck = ""flowey"";
    else if (_ts == snd_txtsans || _ts == snd_txtsans2) _ck = ""sans"";
    else if (_ts == snd_txtpap) _ck = ""papyrus"";
    else if (_ts == snd_txtund || _ts == snd_txtund2 || _ts == snd_txtund3 || _ts == snd_txtund4 || _ts == snd_txtund_hyper) _ck = ""undyne"";
    else if (_ts == snd_txtal) _ck = ""alphys"";
    else if (_ts == snd_txtasg) _ck = ""asgore"";
    else if (_ts == snd_txtasr || _ts == snd_txtasr2) _ck = ""asriel"";
    else if (_ts == snd_mtt1) _ck = ""mettaton"";
    else if (_ts == snd_tem) _ck = ""temmie"";
    else if (_ts == snd_wngdng1) _ck = ""gaster"";
}
var _desc = """";
if (_ck != """")
{
    if (!variable_global_exists(""nvda_seen_chars"")) global.nvda_seen_chars = ""|"";
    if (string_pos(""|"" + _ck + ""|"", global.nvda_seen_chars) == 0)
    {
        global.nvda_seen_chars += _ck + ""|"";
        global.nvda_opt_dirty = 1;   // persist the seen-set via obj_time's deferred ini flush
        if (_ck == ""flowey"") _desc = ""A small golden flower pokes up through the ground. It has a round white face, two big dark eyes and a wide, cheerful smile."";
        else if (_ck == ""toriel"") _desc = ""A tall, gentle monster who looks like a goat standing on two legs. She has soft white fur, long floppy ears, small horns and warm eyes, and wears a long purple robe marked with a white winged crest."";
        else if (_ck == ""sans"") _desc = ""A short, pudgy skeleton with a wide, permanent grin. He wears a blue hooded jacket over a white shirt, black shorts, and pink slippers, and looks completely relaxed."";
        else if (_ck == ""papyrus"") _desc = ""A very tall, lanky skeleton striking a dramatic pose. He wears a homemade costume: a white chest piece with a long red scarf, red gloves and red boots."";
        else if (_ck == ""undyne"") _desc = ""A tall, powerful fish-like monster with deep blue scales, a long red ponytail and sharp yellow teeth. One eye is covered by a black eyepatch, and she wears gleaming metal armour."";
        else if (_ck == ""alphys"") _desc = ""A short, round, yellow lizard-like monster with glasses and a slightly nervous expression. She wears a white lab coat."";
        else if (_ck == ""asgore"") _desc = ""An enormous, powerful monster like a goat standing upright, built like a broad-shouldered king. He has white fur, long curved horns, a golden mane and beard, and wears purple armour."";
        else if (_ck == ""asriel"") _desc = ""A young goat-like monster child with soft white fur, long floppy ears and small horns. He wears a green robe with a single yellow stripe across the middle."";
        else if (_ck == ""mettaton"") _desc = ""A robot shaped like a rectangular metal box balanced on a single wheel. Its front is covered in dials, buttons and a small screen, and two slim white-gloved arms reach out from its sides."";
        else if (_ck == ""temmie"") _desc = ""A strange little creature drawn in a crude, scribbly style: a cat-like face with big ears and wide eyes on a small brown furry body."";
        else if (_ck == ""gaster"") _desc = ""A tall, shadowy figure that seems to melt at the edges. Its pale, cracked face has one crack running up from an eye and another running down, and it speaks in strange symbols."";
    }
}
";

string bridgeInit = @"
if (!variable_global_exists(""nvda_ready""))
{
    global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
    global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
    external_call(global.nvda_init, """");
    global.nvda_ready = 1;
}";

var importGroup = new UndertaleModLib.Compiler.CodeImportGroup(Data);

// Create: init the bridge, (optionally) speak a first-encounter character description,
// then speak line 0.  When a description plays it goes out with interrupt and the line is
// QUEUED after it (so the line isn't cut off); otherwise the line speaks normally.
string queueDef = "if (!variable_global_exists(\"nvda_speak_queue\")) global.nvda_speak_queue = external_define(\"nvda_gm.dll\", \"gmnvda_speak_queue\", 0, 0, 1, 1);\n";
importGroup.QueueAppend(Data.Code.ByName("gml_Object_obj_base_writer_Create_0"),
    "if (variable_global_exists(\"nvda_opt_speech\") && global.nvda_opt_speech == 0) exit;\n"
    + bridgeInit + "\n" + queueDef + "nvda_lasttext = mystring[0];\n"
    + charIntroGml
    + "if (_desc != \"\")\n{\n    external_call(global.nvda_speak, _desc);\n    " + SpeakVia("mystring[0]", "global.nvda_speak_queue") + "\n}\nelse\n{\n    " + SpeakCore("mystring[0]") + "\n}");

// Step (every frame): speak whenever the displayed line TEXT changes.  Tracking the text
// (not just stringno) is essential: after an overworld choice the SAME writer is reused via
// scr_msgup (global.msc++, stringno reset to 0, mystring refilled with the result) - stringno
// never changes, so a stringno-only watcher missed the result text ("You took a piece of
// candy", spider-shop purchase lines, etc.).  Comparing the line text catches that reload
// AND normal stringno advances.
importGroup.QueueAppend(Data.Code.ByName("gml_Object_obj_base_writer_Step_0"),
    "if (variable_global_exists(\"nvda_opt_speech\") && global.nvda_opt_speech == 0) exit;\n"
    + "if (variable_instance_exists(id, \"nvda_lasttext\") && mystring[stringno] != nvda_lasttext)\n{\n    nvda_lasttext = mystring[stringno];\n"
    + SpeakCore("mystring[stringno]") + "\n}");

// OBJ_NOMSCWRITER (the enemy-turn speech-bubble writer) defines its OWN Create_0
// that does NOT call event_inherited(), so the base-writer hook above never runs
// for it -> creatures' turn comments went unspoken.  Hook its Create directly with
// the same logic.  Its Step_0 is inherited from obj_base_writer, so the text-change
// watcher above already covers advancing lines once nvda_lasttext is set here.
importGroup.QueueAppend(Data.Code.ByName("gml_Object_OBJ_NOMSCWRITER_Create_0"),
    "if (variable_global_exists(\"nvda_opt_speech\") && global.nvda_opt_speech == 0) exit;\n" + bridgeInit + "\nnvda_lasttext = mystring[0];\n" + SpeakCore("mystring[0]"));

importGroup.Import();
Console.WriteLine("Injected NVDA: base_writer Create_0 + Step_0 watcher + NOMSCWRITER Create_0");

}

// ===== cutscene audio descriptions =====
{
// Fires a written description at specific story beats to convey what a sighted player SEES but
// the dialogue does not say (a character attacking, a rescue, an environmental event). There is
// no generic "a cutscene is happening" signal in Undertale, so each beat is detected either by
// entering a specific room (RoomBeat) or by a scene-only object coming into existence, rising
// edge (ObjBeat). Each beat fires ONCE per save: a persisted seen-set (nvda_cs_seen, section
// "descriptions" key "cutscenes"), cleared on New Game, same machinery as the character intros.
// Appended to obj_time Step_1 (persistent controller, runs in every room including battle rooms).
Data.Functions.EnsureDefined("external_define", Data.Strings);
Data.Functions.EnsureDefined("external_call", Data.Strings);
Data.Functions.EnsureDefined("variable_global_exists", Data.Strings);
Data.Functions.EnsureDefined("string_pos", Data.Strings);
Data.Functions.EnsureDefined("instance_exists", Data.Strings);
Data.Functions.EnsureDefined("keyboard_check_pressed", Data.Strings);
Data.Functions.EnsureDefined("ord", Data.Strings);
// Diagnostic cutscene-logger builtins (silent recon tool, see the detector block below).
Data.Functions.EnsureDefined("file_text_open_append", Data.Strings);
Data.Functions.EnsureDefined("file_text_write_string", Data.Strings);
Data.Functions.EnsureDefined("file_text_writeln", Data.Strings);
Data.Functions.EnsureDefined("file_text_close", Data.Strings);
Data.Functions.EnsureDefined("room_get_name", Data.Strings);
Data.Functions.EnsureDefined("string", Data.Strings);

// CsSpeak: speak <text> as PRIORITY - interrupt if nothing is currently protected, else queue
// behind it - and open a priority window so dialogue that immediately follows (e.g. Flowey's
// "die" line) QUEUES behind the description instead of cutting it off. Window measured in frames
// via global.nvda_now (obj_time ticks it below); 180 = ~6 s at 30 fps. SpeakVia honours it too.
string CsSpeak(string text) =>
  " var _csfn = global.nvda_speak;" +
  " if (variable_global_exists(\"nvda_prio_until\") && variable_global_exists(\"nvda_speak_queue\") && global.nvda_now < global.nvda_prio_until) _csfn = global.nvda_speak_queue;" +
  " external_call(_csfn, \"" + text + "\");" +
  " global.nvda_prio_until = global.nvda_now + 180;";
// EdgeBeat: fire on the RISING edge of <cond> (false->true), re-arming when <cond> goes false.
// So it fires once each time you ENTER the room / the scene object appears, and REPLAYS on every
// re-entry (NOT once-per-save) - important so a player who resets a puzzle room hears the
// description again. Per-beat state kept in global.nvda_b_<key>.
string EdgeBeat(string key, string cond, string text) =>
  "    if (" + cond + ") { if (!variable_global_exists(\"nvda_b_" + key + "\") || global.nvda_b_" + key + " == 0) { global.nvda_b_" + key + " = 1;" + CsSpeak(text) + " } } else global.nvda_b_" + key + " = 0;\n";
string ObjBeat(string key, string objExpr, string text) => EdgeBeat(key, "instance_exists(" + objExpr + ")", text);
string RoomBeat(string key, string roomCond, string text) => EdgeBeat(key, roomCond, text);
// FallBeat: fire on the FALLING edge of <objExpr> existing (present last frame, gone now) but only
// if we did NOT just change rooms - i.e. the object was destroyed while you stayed put, e.g. an NPC
// finishing a scene and leaving. Used for "a character floats away after the dialogue ends".
string FallBeat(string key, string objExpr, string text) =>
  "    { var _ex = instance_exists(" + objExpr + "); if (variable_global_exists(\"nvda_fb_" + key + "\") && global.nvda_fb_" + key + " == 1 && _ex == 0 && global.nvda_roomchg == 0) {" + CsSpeak(text) + " } global.nvda_fb_" + key + " = _ex; }\n";
// ActorStateBeat: fire on the rising edge of a per-INSTANCE state (a scene counter, a sprite, etc.),
// read SAFELY inside a nested instance_exists guard so we never touch a member on a missing instance
// (GMS1 has no reliable && short-circuit, and member-access on a dead object index is a FATAL error).
// <objName> = the object; <cond> = a bool GML expr on it (e.g. "obj_x.cn > 0"). Use this - NOT plain
// instance_exists - for scene beats whose actor is CREATED at room load but only ACTS later (the actor
// existing != the scene happening). Re-arms when the state is false or the instance is gone.
string ActorStateBeat(string key, string objName, string cond, string text) =>
  "    if (instance_exists(" + objName + ") == 0) { if (variable_global_exists(\"nvda_b_" + key + "\")) global.nvda_b_" + key + " = 0; }\n" +
  "    else { if (!variable_global_exists(\"nvda_b_" + key + "\")) global.nvda_b_" + key + " = 0;\n" +
  "        if ((" + cond + ") && global.nvda_b_" + key + " == 0) { global.nvda_b_" + key + " = 1;" + CsSpeak(text) + " }\n" +
  "        else if ((" + cond + ") == 0) global.nvda_b_" + key + " = 0; }\n";
// AmbBeat: an ENVIRONMENTAL room description. Whenever you are in <roomCond> it records the text as
// global.nvda_roomdesc (so the L key can re-speak the current room on demand), and speaks it just
// ONCE per save on first visit (tracked in the persisted global.nvda_amb_seen). This is the
// "describe every room" primitive. The seen-set is loaded/flushed by the options block; until it is
// loaded the beat still sets nvda_roomdesc but holds the one-time speak (variable_global_exists gate).
string AmbBeat(string key, string roomCond, string text) =>
  "    if (" + roomCond + ") { global.nvda_roomdesc = \"" + text + "\";" +
  " if (variable_global_exists(\"nvda_amb_seen\") && string_pos(\"|" + key + "|\", global.nvda_amb_seen) == 0) { global.nvda_amb_seen += \"" + key + "|\"; global.nvda_opt_dirty = 1;" + CsSpeak(text) + " } }\n";

string beats = "";
// ===== EVENT beats: something HAPPENS (an attack, a rescue, an NPC leaving). Fire on the event,
//       not per-room. Kept as Obj/Fall beats. =====
// Flowey's friendly act drops exactly when obj_radialfakegen (the pellet ring) spawns.
beats += ObjBeat("flowey_attack", "obj_radialfakegen",
    "Flowey's cheerful face suddenly twists into something cruel. The pellets turn on you, forming a ring around your soul that closes in to surround you.");
beats += ObjBeat("toriel_rescue", "obj_torielcutscene",
    "A ball of fire streaks in and strikes the flower, knocking it clean out of sight. A tall, motherly monster steps out of the shadows toward you.");
beats += ObjBeat("toriel_fight", "obj_torielboss",
    "Toriel stands blocking the great stone door, and flames bloom in her hands. Yet each time she attacks, her fire curves away from you at the last moment. She cannot bring herself to truly hurt you.");
// The spare goodbye is done by the OVERWORLD actor obj_toroverworld3 in room_basement4 (NOT the battle
// object obj_torielboss, which dies during the battle->overworld fade). It shows spr_toriel_hug at the
// embrace. Fire on the rising edge of the hug sprite, checked INSIDE an instance_exists guard (separate
// nested ifs, so we never read .sprite_index on a missing instance). Re-arms when not hugging.
beats += "    if (room == room_basement4 && instance_exists(obj_toroverworld3)) {\n" +
    "        var _hug = (obj_toroverworld3.sprite_index == spr_toriel_hug || obj_toroverworld3.sprite_index == spr_toriel_hug2 || obj_toroverworld3.sprite_index == spr_toriel_hug3);\n" +
    "        if (!variable_global_exists(\"nvda_b_torgood\")) global.nvda_b_torgood = 0;\n" +
    "        if (_hug && global.nvda_b_torgood == 0) { global.nvda_b_torgood = 1;" +
    CsSpeak("Toriel wraps you in a warm embrace, holding you close for a long moment. Then she lets go, turns away, and walks off down the corridor, leaving you to go on alone.") +
    " } else if (_hug == 0) global.nvda_b_torgood = 0;\n" +
    "    }\n";
// ghost-2 fades out where he sits after his dialogue (his Step fades image_alpha to 0 + self-destroy).
beats += FallBeat("ghost2_leaves", "obj_napstablook2",
    "The little ghost slowly fades, growing fainter and fainter, until he has vanished from sight.");
// obj_darksanstrigger is placed in the room (exists from entry); the approaching Sans actor
// obj_darksans1 is created only on COLLISION (when you walk into the trigger) = the real moment.
beats += ObjBeat("sans_meet", "obj_darksans1",
    "The path ahead is dark and silent. Behind you, soft footsteps approach, and a short, shadowy figure steps up close and holds out a hand toward you.");
// ===== UNDYNE ARC (Waterfall + Hotland). She is SILENT in these scenes, so dialogue narration gives
//       a blind player nothing - this is pure visual action. Keyed on room + the actual actor object
//       (obj_undynea_actor / _actor2 / obj_undynefall), which only exist while the scene is playing,
//       so no misfire on backtrack (the room-placed trigger persists; the actor does not). =====
// Timed on each encounter's internal counter going nonzero (= you walked PAST the trigger and the
// scene fires), NOT the actor merely existing (it is created invisible at room load). Encounter 2
// sets cn=1 as pre-setup, so its real start is cn >= 2 (control locks, Undyne begins the grab).
beats += ActorStateBeat("undyne_ledge", "obj_undyneencounter1", "obj_undyneencounter1.cn > 0",
    "A spear of blue light slams into the ground beside you. High on the ledge above stands a tall armoured figure, glaring down at you through a horned helmet.");
beats += ActorStateBeat("undyne_grass", "obj_undyneencounter2", "obj_undyneencounter2.cn >= 2",
    "You crouch hidden in the tall grass. Heavy armoured footsteps approach and stop right beside you. A gauntleted hand reaches down, closes around the small monster child at your side, lifts him up by the head - then sets him down and strides away.");
beats += ActorStateBeat("undyne_bridge", "obj_undyneencounter3", "obj_undyneencounter3.cn > 0",
    "The armoured warrior drops onto the bridge behind you and gives chase, hurling spear after spear of blue light as you run.");
beats += ActorStateBeat("undyne_fall", "obj_undyneencounter4", "obj_undyneencounter4.con > 0",
    "Cornered at the end of the bridge, you can only back away as she advances - until the walkway gives out beneath you and you plunge down into the dark.");
beats += ObjBeat("undyne_collapse", "obj_undynefall",
    "Overcome by the sweltering heat inside her heavy armour, the warrior sways, staggers, and at last crashes to the ground, where she lies still.");

// ===== BACK-HALF STORY CUTSCENES (Hotland -> CORE -> New Home -> neutral finale). Silent visual
//       action a blind player would otherwise miss, authored from the cutscene log of Lilian's
//       neutral playthrough (2026-07-08). Each keyed on the beat-MOMENT scene object (per the
//       "the actor existing != the scene happening" rule) and room-gated for safety. =====

// Mettaton's box form transforms into his glamorous EX body (obj_scrollaway_event = the dramatic
// scroll-up reveal spawned at the transformation; his box form was already covered on first meeting).
beats += EdgeBeat("mett_ex", "room == room_fire_core_metttest && instance_exists(obj_scrollaway_event)",
    "In a burst of light and confetti the boxy robot's true form unfolds: a tall, glamorous machine with long slender legs, one arm flung out in a pose, dark hair swept over one eye, and a glowing heart set in his chest. Mettaton EX has arrived, and he is fabulous.");

// Cooking show: Mettaton launches his whole counter into the sky and you chase it up on a jetpack.
// NOTE: this is also a minigame Lilian failed -> see the gameplay-guidance track (memory) for later.
beats += EdgeBeat("cook_jetpack", "room == room_fire_cookingshow && instance_exists(obj_phonetojetpack)",
    "The entire cooking counter blasts up off the stage and rockets into the sky, Mettaton riding it away. A phone rings, a jetpack straps itself to your back, and you shoot upward after him, hurtling through the open air.");

// Throne room: the King watering the golden flowers, back turned, before he notices you (con>=2 =
// the scene has triggered; he turns to face you later at con 19+, which his first line covers).
beats += ActorStateBeat("asgore_flowers", "obj_asgoremeet_event", "obj_asgoremeet_event.con >= 2",
    "At the far end of the hall a huge, broad figure in royal robes stands with his back to you, great curved horns bowed as he gently waters the bed of golden flowers, humming softly to himself, not yet aware that anyone has come.");

// Asgore's fight begins: he destroys the MERCY button so you cannot spare him (obj_asgoreb Create
// sets obj_sparebt.visible = false = the mechanical realisation of the smash).
beats += EdgeBeat("asgore_nomercy", "room == room_castle_barrier && instance_exists(obj_asgoreb)",
    "The King lifts his great trident and brings it down on the MERCY button, smashing it to splinters. There is no talking your way out now, and no sparing him - only the fight.");

// The six human souls rise into the air at the barrier (barrierevent spawns obj_heartcontainer).
beats += EdgeBeat("barrier_souls", "room == room_castle_barrier && instance_exists(obj_heartcontainer)",
    "Six upturned hearts drift up into the dark and hang in the air around you, each a different colour: cyan, orange, blue, purple, green and gold. The six human souls the King has gathered, all that is needed to break the barrier at last.");

// Finale (room_f_room): the flower shatters your SAVE file (obj_savepoint_fake.crack counts up as
// each blow lands, then the file is erased and Flowey's grinning face rises).
beats += ActorStateBeat("finale_erase", "obj_savepoint_fake", "obj_savepoint_fake.crack >= 1",
    "The whole screen lurches and cracks. Three thunderous blows split it like glass, and your very SAVE file breaks apart and is wiped away into nothing. Out of the darkness rises a small golden flower, wearing a wide, grinning face.");

// Finale: Photoshop / Omega Flowey reveal (obj_floweybattler2 = the boss).
beats += EdgeBeat("finale_photoshop", "room == room_f_room && instance_exists(obj_floweybattler2)",
    "The flower has fused himself with your stolen SAVE and the six souls into something monstrous: a towering hulk of steel, cables and television screens, machinery grinding and flashing all around a huge, distorted face that leers out from the very centre of it. He looms over you, vast and grinning.");

// Finale aftermath: the six souls turn on Flowey and protect you (obj_6soul_helpcutscene).
beats += ObjBeat("finale_souls_help", "obj_6soul_helpcutscene",
    "One by one the six human souls flare back to life inside the machine and turn against the flower, circling around you, shielding you and healing your wounds as they rise up against their captor.");

// ===== ROOM AMBIANCE: environmental descriptions. Spoken ONCE per save on first visit; the L key
//       re-speaks the current room any time (AmbBeat records global.nvda_roomdesc). All verified
//       against _rooms_all.txt. =====
// ---- Ruins ----
beats += AmbBeat("amb_area1", "room == room_area1 || room == room_area1_2",
    "You land unhurt in a bed of golden flowers, deep underground. Pale light filters down from a hole high above.");
beats += AmbBeat("amb_ruins2", "room == room_ruins2",
    "A small stone chamber. A pressure plate is set into the floor, and a lever juts from the wall. Pressing both opens the door ahead.");
beats += AmbBeat("amb_ruins3", "room == room_ruins3",
    "A room crossed by a bed of spikes. Rows of switches line the walls, and Toriel has marked the ones you need with arrows, so you can lower the spikes and cross safely.");
beats += AmbBeat("amb_ruins4", "room == room_ruins4",
    "A cloth training dummy stands on a wooden stand in the middle of the room, its stitched face blank and patient.");
beats += AmbBeat("amb_ruins5", "room == room_ruins5",
    "A long hall. Patches of spikes stud the floor, with a safe path winding between them.");
beats += AmbBeat("amb_ruins6", "room == room_ruins6",
    "A long, straight corridor stretches away into the gloom, its far end lost in shadow. The air is still, and your own quiet footsteps are the only sound.");
beats += AmbBeat("amb_ruins7a", "room == room_ruins7A",
    "A bowl of colourful sweets rests on a pedestal here, beside a small sign.");
beats += AmbBeat("amb_ruins9", "room == room_ruins9",
    "A rock sits on the floor near a stretch of spikes. Push it onto the switch to hold the spikes down, then cross while they are lowered.");
beats += AmbBeat("amb_ruins10", "room == room_ruins10",
    "A quiet room with holes worn in the floor and two stone plaques set into the walls, their old inscriptions waiting to be read.");
beats += AmbBeat("amb_ruins11", "room == room_ruins11",
    "A large grey rock rests on the floor, and set into the ground nearby is a switch, a pressure plate. To move the rock, walk into it from the opposite side and it slides one space at a time. Push it onto the switch to open the way forward.");
beats += AmbBeat("amb_ruins12", "room == room_ruins12",
    "A small white ghost lies stretched across the path ahead, faint and half-see-through. The letters z, z, z drift up from him as he pretends to be asleep, though he does not look like he is fooling anyone.");
beats += AmbBeat("amb_ruins12a", "room == room_ruins12A",
    "A wedge of cheese sits on a table in the middle of the room, stuck fast after being left untouched for ages. In the far wall, a tiny mouse hole waits in hopeful silence.");
beats += AmbBeat("amb_ruins14", "room == room_ruins14",
    "A tall room. Just ahead, a hole in the floor drops down to a lower level. Down below there is a switch set into the floor, a couple of plump vegetable monsters half-buried in the earth, a faded ribbon lying on the ground, and a small, mournful ghost resting quietly in one of the hollows.");
beats += AmbBeat("amb_ruins15", "room == room_ruins15A",
    "A dim room dotted with tall pillars. Coloured switches - one red, one green, one blue - are set among them, and spikes block the way onward until the right ones are pressed.");
beats += AmbBeat("amb_ruins18", "room == room_ruins18OLD",
    "A small toy knife lies on the ground here, waiting to be picked up.");
// ---- Toriel's home ----
beats += AmbBeat("amb_home_yard", "room == room_ruins19",
    "A large, leafless black tree stands in a small yard, its bare branches spread wide and dead leaves scattered all around. Just beyond it, a cosy home is built into the rock, warm light spilling from its windows.");
beats += AmbBeat("amb_home_entrance", "room == room_torhouse1",
    "You step inside Toriel's home, warm and snug after the cold stone of the Ruins. A staircase leads down to your left, and a hallway opens away to your right.");
beats += AmbBeat("amb_home_living", "room == room_torhouse2",
    "A cosy living room. A fire crackles in the hearth, and a large, cushioned reading chair sits beside it with a book left open on its arm. A doorway leads through to a small kitchen.");
beats += AmbBeat("amb_home_hallway", "room == room_torhouse3",
    "A long hallway lined with doors, softly lit. A mirror hangs on one wall and a little lamp glows in a corner. One of the doors has been made up as a bedroom, just for you.");
// ---- Rooms Lilian flagged as missing on her Ruins walk (all verified against the room dump) ----
beats += AmbBeat("amb_ruins1", "room == room_ruins1",
    "A tall entrance hall of deep purple stone, where a wide staircase leads up and deeper into the Ruins. A patch of soft light marks a save point here.");
// The leaf pile: the room immediately after the "unnecessary tension" corridor (candy bowl above,
// first rock puzzle to the right) = room_ruins7, which also holds a frog and a save point.
beats += AmbBeat("amb_ruins7", "room == room_ruins7",
    "A room carpeted in a deep pile of dry red leaves that crinkle softly underfoot. A small frog-like monster rests by the wall, and a save point glows nearby. Passages lead away, one going up and one to the right.");
beats += AmbBeat("amb_ruins8", "room == room_ruins8",
    "A tall, narrow room with a hole in the floor that you can drop down through to the level below.");
beats += AmbBeat("amb_ruins13", "room == room_ruins13",
    "A room where a couple of small frog-like monsters rest by the walls. They will happily share advice, and a sign nearby explains how sparing a monster, pausing, and skipping through text all work.");
beats += AmbBeat("amb_ruins16", "room == room_ruins16",
    "A wider stone room where several passages meet.");
beats += AmbBeat("amb_ruins17", "room == room_ruins17",
    "A small frog-like monster sits quietly here, ready to offer a word of advice if you speak to it.");
beats += AmbBeat("amb_ruins12b", "room == room_ruins12B",
    "Two spider webs are strung across a corner of this room, one small and one large, with a little sign beside them. It is a spider bake sale: leave a few coins in a web and the spiders will sell you a treat.");
// ---- Toriel's home: the two bedrooms off the hallway ----
beats += AmbBeat("amb_home_bedroom", "room == room_asrielroom",
    "A child's cosy bedroom. A neatly made bed sits beneath a soft lamp, with a box of toys and a few small comforts about the room. It has been made ready just for you.");
beats += AmbBeat("amb_home_torielroom", "room == room_torielroom",
    "Toriel's own bedroom, tidy and warm. A small chair sits in one corner, and there are a few of her personal things to read if you look around.");
// ---- The basement corridor Toriel leads you down at the end of the Ruins ----
beats += AmbBeat("amb_basement1", "room == room_basement1",
    "A long, cold basement corridor of grey stone, leading away from the warmth of the house above.");
beats += AmbBeat("amb_basement2", "room == room_basement2",
    "The cold stone corridor stretches on, quiet and dim.");
beats += AmbBeat("amb_basement3", "room == room_basement3",
    "The passage narrows, the air growing colder the further you go.");
beats += AmbBeat("amb_basement4", "room == room_basement4",
    "A short stretch of corridor, the worn stone smooth underfoot.");
beats += AmbBeat("amb_basement5", "room == room_basement5",
    "A very long, straight passage stretching far ahead into the cold. At its distant end stands a great doorway leading out of the Ruins.");
// ---- Snowdin: the snowy forest (all verified against _rooms_all.txt, room_tundra*) ----
beats += AmbBeat("amb_tundra1", "room == room_tundra1",
    "The great stone door of the Ruins closes behind you, and ahead the path opens into a hushed, snow-covered forest. Bare trees crowd close on either side, and your breath fogs in the cold, still air.");
beats += AmbBeat("amb_tundra2", "room == room_tundra2",
    "A long path winding through snowy woods. A large branch lies fallen across the way, and further on stands a strange wooden gate, its bars set so far apart that anyone could simply walk between them. A conveniently shaped lamp sits off to one side.");
beats += AmbBeat("amb_tundra3", "room == room_tundra3",
    "A clearing in the woods. A wooden sentry station, little more than a guard post of bars and a counter, stands beside the path. A save point glows softly nearby, next to a small sign.");
beats += AmbBeat("amb_tundra3a", "room == room_tundra3A",
    "A small frozen pond tucked off the main path, its surface dotted with dark divots in the ice. A fishing rod has been left propped at the water's edge, its line trailing out across the frozen surface.");
beats += AmbBeat("amb_tundra4", "room == room_tundra4",
    "A snowy stretch of path with another sentry station off to the side. A small, sharp-dressed bird monster loiters nearby, and telephone wires hum faintly overhead.");
beats += AmbBeat("amb_tundra5", "room == room_tundra5",
    "A guard post built like a little dog house sits beside the path here. A dog treat lies within reach, and a small bell hangs at the counter.");
beats += AmbBeat("amb_tundra6", "room == room_tundra6",
    "A wide, open stretch where a broad sheet of slippery ice covers much of the floor. Stepping onto the ice sends you sliding until you reach solid ground again. A sign stands near the path.");
beats += AmbBeat("amb_tundra6a", "room == room_tundra6A",
    "A quiet little clearing off the path. A small snowman stands here in the drifts, round and patient, as if waiting for someone to speak to him.");
beats += AmbBeat("amb_tundra7", "room == room_tundra7",
    "A snowy field crossed by an invisible maze of electric barriers. A trail of footprints has been left in the snow, marking out a safe path through it - follow the tracks to cross without being shocked.");
beats += AmbBeat("amb_tundra8", "room == room_tundra8",
    "A broad snowfield opens up here. A vendor stands near the entrance selling frozen treats, and off across the snow a course has been marked out for a game of rolling a great snowball toward a distant hole. A lone tree stands at the edge of the clearing.");
beats += AmbBeat("amb_tundra8a", "room == room_tundra8A",
    "A small side clearing with a pair of dog houses standing side by side, a sign posted between them.");
beats += AmbBeat("amb_tundra9", "room == room_tundra9",
    "A snowy path where a large sheet of paper lies on the ground, printed with a word puzzle - a crossword, or perhaps a jumble - left behind mid-argument.");
beats += AmbBeat("amb_tundra_spaghetti", "room == room_tundra_spaghetti",
    "A table has been set up in the snow, a plate of spaghetti frozen solid upon it beside a microwave that is not plugged into anything. A save point glows nearby, and a tiny mouse hole waits hopefully in the far wall.");
beats += AmbBeat("amb_tundra_snowpuzz", "room == room_tundra_snowpuzz",
    "A large puzzle room. Deep snow blankets the floor, with a bed of spikes set into it and tall trees dotted about. A safe path is hidden in the snow, and a sign nearby offers a clue to finding the way across.");
beats += AmbBeat("amb_tundra_xoxosmall", "room == room_tundra_xoxosmall",
    "A puzzle room. Tiles marked with an X are set into the floor. Step on each one to switch it to an O; turn them all and the spikes ahead lower, opening the way. A sign nearby explains the rules.");
beats += AmbBeat("amb_tundra_xoxopuzz", "room == room_tundra_xoxopuzz",
    "A larger version of the X and O puzzle, its floor covered in marked tiles and guarded by spikes. Step on each X to turn it into an O, clearing them all to lower the spikes and pass.");
beats += AmbBeat("amb_tundra_randoblock", "room == room_tundra_randoblock",
    "A room whose floor is a grid of brightly coloured tiles, with a switch set into the wall on the far side that controls the puzzle. For all its complicated-sounding rules, the way ahead opens easily enough.");
beats += AmbBeat("amb_tundra_lesserdog", "room == room_tundra_lesserdog",
    "Another sentry station stands here, guarded by a dog in armour whose neck can stretch impossibly long. A save point glows nearby, and a dog house sits against the wall.");
beats += AmbBeat("amb_tundra_icehole", "room == room_tundra_icehole",
    "A small alcove where two lumpy snow sculptures have been built, rough likenesses of two skeletons. A hole in the floor here drops down to somewhere below.");
beats += AmbBeat("amb_tundra_iceentrance", "room == room_tundra_iceentrance",
    "A long, cavernous room of ice. Slippery patches send you sliding when you step on them, and holes gape in the floor ready to drop you to the level below. Pick a path carefully between them.");
beats += AmbBeat("amb_tundra_iceexit_new", "room == room_tundra_iceexit_new",
    "The far end of the icy cavern, its walls glittering with frost. A shaggy, antlered monster stands off to one side, and the way out leads on toward warmer ground.");
beats += AmbBeat("amb_tundra_iceexit", "room == room_tundra_iceexit",
    "A short passage leading out of the ice caves. Far off in the snowy distance, the tiny shape of a house can be seen, hinting at the town ahead.");
beats += AmbBeat("amb_tundra_poffzone", "room == room_tundra_poffzone",
    "A snowy hollow scattered with soft mounds of powder. A small dog house sits here, and a dog snuffles happily about among the drifts.");
beats += AmbBeat("amb_tundra_dangerbridge", "room == room_tundra_dangerbridge",
    "A long rope bridge stretches across a deep, fog-filled chasm. An array of menacing contraptions - a dog, cannons, spears, and a flamethrower - has been rigged at the far end. Cross the bridge to go on.");
beats += AmbBeat("amb_tundra_town", "room == room_tundra_town",
    "Snowdin town: a long, cheerful street of wooden buildings strung with coloured lights, a decorated tree glowing at its heart. Shops, an inn, and a warm-looking diner line the way, townsfolk milling about in the snow. At the far end stand two mailboxes and a house.");
beats += AmbBeat("amb_tundra_town2", "room == room_tundra_town2",
    "The quieter edge of town, past the last of the buildings. A wolf heaves blocks of ice one by one onto a conveyor that carries them off into the water, and a family of small slime monsters lingers nearby.");
beats += AmbBeat("amb_tundra_dock", "room == room_tundra_dock",
    "A wooden dock at the water's edge, dark water lapping quietly against the boards. A strange, hooded boatman waits here, ready to carry you onward if you ask.");
beats += AmbBeat("amb_tundra_inn", "room == room_tundra_inn",
    "The cosy lobby of the Snowdin inn. A front desk stands to one side, the innkeeper waiting behind it, ready to rent you a room for the night.");
beats += AmbBeat("amb_tundra_inn_2f", "room == room_tundra_inn_2f",
    "A snug little bedroom upstairs in the inn. A bed waits invitingly; a good rest here will mend your wounds.");
beats += AmbBeat("amb_tundra_grillby", "room == room_tundra_grillby",
    "Grillby's, the town's warm and lively diner. Booths and tables fill the room, a crowd of dogs and other regulars gathered about, and behind the bar stands the owner - a quiet monster made of living fire.");
beats += AmbBeat("amb_tundra_library", "room == room_tundra_library",
    "The town library - though the sign outside spells it librarby. Rows of shelves and reading tables fill the room, and a few studious monsters look up from their work as you enter.");
beats += AmbBeat("amb_tundra_garage", "room == room_tundra_garage",
    "A cluttered garage. A dog bed, a food bowl, and a well-chewed toy lie about the floor, and a strange barred contraption stands against one wall.");
beats += AmbBeat("amb_tundra_sanshouse", "room == room_tundra_sanshouse",
    "The front room of the skeleton brothers' house, cosy and lived-in. A couch faces a television, a kitchen opens off to the right, and doors lead away to the bedrooms.");
beats += AmbBeat("amb_tundra_paproom", "room == room_tundra_paproom",
    "A bedroom belonging to the taller brother. A race-car bed sits against one wall, along with a computer, a bookshelf, a box of bones, and an action figure posed on a small table.");
beats += AmbBeat("amb_tundra_sansroom", "room == room_tundra_sansroom",
    "The shorter brother's bedroom, and a spectacular mess. Clothes and rubbish cover the floor, a self-sustaining tornado of trash spins quietly in one corner, and a treadmill sits buried and unused.");
beats += AmbBeat("amb_tundra_sansroom_dark", "room == room_tundra_sansroom_dark",
    "A wide, pitch-dark room. Something waits in the blackness ahead, just out of sight.");
beats += AmbBeat("amb_tundra_sansbasement", "room == room_tundra_sansbasement",
    "A hidden workshop behind a locked door, dusty and long forgotten. Odd papers are pinned to the walls, and a large object rests under a cloth in the corner.");
// ---- Waterfall: the deep blue caverns (verified against _rooms_all.txt, room_water*) ----
beats += AmbBeat("amb_water1", "room == room_water1",
    "You leave the snow behind and step into Waterfall, a vast cavern of deep blue. Water trickles and echoes all around, glowing plants light the gloom, and the air turns warm and damp.");
beats += AmbBeat("amb_water2", "room == room_water2",
    "A quiet ledge where a wooden sentry station stands, unattended and suspiciously casual. A save point glows nearby, and a single blue echo flower grows here, softly repeating the last thing whispered to it.");
beats += AmbBeat("amb_water3", "room == room_water3",
    "A waterfall spills down one wall into a channel of flowing water. A large rock rests on the bank; push it into the stream to block the current so you can cross. A sign and an echo flower stand nearby.");
beats += AmbBeat("amb_water3a", "room == room_water3A",
    "A small alcove lit by a pair of tall, glowing blue mushrooms.");
beats += AmbBeat("amb_water4", "room == room_water4",
    "A thick patch of tall grass grows in the middle of this room, high enough to hide in. On a ledge far above, a tall figure in armour stands watching, utterly still. A save point glows at the far side.");
beats += AmbBeat("amb_water_bridgepuzz1", "room == room_water_bridgepuzz1",
    "A bridge-seed puzzle. Clusters of seeds drift in the water; walk into them to push them together, and where they gather they sprout into solid lily-pad bridges you can cross. A sign nearby explains it.");
beats += AmbBeat("amb_water5", "room == room_water5",
    "A larger stretch of water strewn with drifting bridge-seeds. Push the seeds together to grow lily-pad bridges across the water. Glowing mushrooms and a bell-shaped blossom light the room, and a sign offers a hint.");
beats += AmbBeat("amb_water5a", "room == room_water5A",
    "A small side room beside the water, lit by the soft glow of an echo flower.");
beats += AmbBeat("amb_water6", "room == room_water6",
    "A breathtaking room. The high ceiling glitters with countless tiny lights like a night sky full of stars, though they are only gems in the rock. Echo flowers grow here, each still whispering a wish that someone once breathed into it, and a telescope stands for gazing up.");
beats += AmbBeat("amb_water7", "room == room_water7",
    "A wooden boardwalk over dark water, where a small, fussy water-loving monster busily scrubs the planks clean. A row of signs lines the path.");
beats += AmbBeat("amb_water8", "room == room_water8",
    "A long wooden boardwalk stretching out across open water.");
beats += AmbBeat("amb_water9", "room == room_water9",
    "A boardwalk with a tall patch of grass growing thick at its edge, just deep enough to hide in.");
beats += AmbBeat("amb_water_savepoint1", "room == room_water_savepoint1",
    "A quiet nook with a save point, a glowing echo flower, and a little mouse hole worn in the wall.");
beats += AmbBeat("amb_water11", "room == room_water11",
    "A dim room where motes of light drift through the air. A telescope stands here for looking out over the water, and a small sentry station sits nearby.");
beats += AmbBeat("amb_water_nicecream", "room == room_water_nicecream",
    "A glowing-mushroom grotto where the Nice Cream vendor has set up his cart, selling frozen treats with kind words written on the wrappers.");
beats += AmbBeat("amb_water12", "room == room_water12",
    "A vast, beautiful cavern glowing deep blue. Shining water pours down the walls, luminous plants drift and sway, and echo flowers whisper here and there. It is one of the loveliest sights in the whole Underground.");
beats += AmbBeat("amb_water_shoe", "room == room_water_shoe",
    "A small clearing among glowing mushrooms, with a patch of tall grass at its heart.");
beats += AmbBeat("amb_water_bird", "room == room_water_bird",
    "A ledge at the edge of a wide gap. A large, gentle bird waits here, willing to carry anyone small enough across to the far side.");
beats += AmbBeat("amb_water_onionsan", "room == room_water_onionsan",
    "A long, shallow channel where the water has sunk worryingly low. A big, floppy, friendly sea creature lingers in the shallows, overjoyed to have someone to talk to.");
beats += AmbBeat("amb_water14", "room == room_water14",
    "A rainy ledge where soft, sorrowful singing drifts on the air. A shy, fish-like monster hovers here, half-hiding behind her own song, and signs stand along the path.");
beats += AmbBeat("amb_water_piano", "room == room_water_piano",
    "A room with an old piano against the wall. Playing the right melody - the tune hinted at elsewhere in the caverns - will open the way to a hidden reward.");
beats += AmbBeat("amb_water_dogroom", "room == room_water_dogroom",
    "A hushed, shrine-like room. A curious old artifact rests here on a pedestal, waiting to be taken.");
beats += AmbBeat("amb_water_statue", "room == room_water_statue",
    "A stone statue sits alone in endless rain, silent and still. A box of umbrellas stands nearby; shelter the statue from the rain and it reveals the gentle music it was always meant to play.");
beats += AmbBeat("amb_water_prewaterfall", "room == room_water_prewaterfall",
    "The rain begins here. A box of umbrellas stands beside a sign - take one to stay dry on the long walk ahead.");
beats += AmbBeat("amb_water_waterfall", "room == room_water_waterfall",
    "A long path through steady rain, waterfalls curtaining down on either side. It is a beautiful, wistful walk.");
beats += AmbBeat("amb_water_waterfall2", "room == room_water_waterfall2",
    "A tall, rainy passage climbing upward, a single echo flower glowing softly along the way.");
beats += AmbBeat("amb_water_waterfall3", "room == room_water_waterfall3",
    "A rainy ledge with a wide, sweeping view: far across the water, the great grey castle of the King rises in the distance - the goal of the whole long journey.");
beats += AmbBeat("amb_water_waterfall4", "room == room_water_waterfall4",
    "A rainy path where a box of umbrellas waits to be refilled. A muscular, horse-like monster flexes and winks nearby.");
beats += AmbBeat("amb_water_preundyne", "room == room_water_preundyne",
    "A quiet ledge in the rain, with a save point. The path ahead climbs onto a high wooden bridge.");
beats += AmbBeat("amb_water_undynebridge", "room == room_water_undynebridge",
    "A high, narrow bridge over a deep chasm, lashed with rain. This is dangerous ground - the armoured figure hunts here, hurling spears from the dark. Keep moving.");
beats += AmbBeat("amb_water_undynebridgeend", "room == room_water_undynebridgeend",
    "The far end of the bridge, hemmed in with nowhere left to run as spears rain down out of the gloom.");
beats += AmbBeat("amb_water_trashzone1", "room == room_water_trashzone1",
    "A dim dump at the foot of the falls, where everything that tumbles into the Underground washes up. Heaps of garbage lie in shallow water, and a water-loving monster potters among them.");
beats += AmbBeat("amb_water_trashsavepoint", "room == room_water_trashsavepoint",
    "A patch of the garbage dump with a save point, standing in shallow water among the heaped rubbish.");
beats += AmbBeat("amb_water_trashzone2", "room == room_water_trashzone2",
    "A tall, junk-filled chamber. Among the piles of trash and a rusted old cooler floats a lumpy, furious dummy, spoiling for a fight.");
beats += AmbBeat("amb_water_friendlyhub", "room == room_water_friendlyhub",
    "A calmer stretch of Waterfall with a save point and a sign. Cosy little houses stand nearby, and a friendly clam-like monster chatters away by the path.");
beats += AmbBeat("amb_water_undyneyard", "room == room_water_undyneyard",
    "The yard outside a bright, fish-shaped house. A door waits to be knocked on - this is the home of the Underground's fierce captain of the guard.");
beats += AmbBeat("amb_water_undynehouse", "room == room_water_undynehouse",
    "The inside of Undyne's fish-shaped house, warm and full of character. A drawer stands against one wall, oddly stuffed with bones, and there are shelves and keepsakes to look over.");
beats += AmbBeat("amb_water_blookyard", "room == room_water_blookyard",
    "A quiet yard with a couple of small houses. A little white ghost drifts here, faint and shy.");
beats += AmbBeat("amb_water_blookhouse", "room == room_water_blookhouse",
    "The ghost's home, small and a touch melancholy. A computer hums in the corner, a fridge stands nearby, and a stack of music CDs waits beside a spot on the floor where you can lie down and feel like garbage together.");
beats += AmbBeat("amb_water_hapstablook", "room == room_water_hapstablook",
    "A neighbouring house, empty and quiet, its shelves lined with old diaries left behind by whoever once dreamed here.");
beats += AmbBeat("amb_water_farm", "room == room_water_farm",
    "A damp little snail farm. Snails inch slowly across their pen, and a small race track waits for anyone who fancies betting on the fastest one.");
beats += AmbBeat("amb_water_prebird", "room == room_water_prebird",
    "A grassy ledge with a thick patch of tall grass and a few signs along the path.");
beats += AmbBeat("amb_water_shop", "room == room_water_shop",
    "A cosy shop carved into the rock, tended by a cheerful old turtle who has watched over the Underground's whole long history.");
beats += AmbBeat("amb_water_dock", "room == room_water_dock",
    "A small dock at the water's edge, where the hooded boatman waits to ferry you onward if you wish.");
beats += AmbBeat("amb_water15", "room == room_water15",
    "A dark cavern flickering with glowing fireflies and drifting echoes, shallow water pooling across the floor.");
beats += AmbBeat("amb_water16", "room == room_water16",
    "A dark room lit only by clusters of glowing mushrooms. Stepping near a mushroom makes it flare bright, lighting the way forward from one to the next.");
beats += AmbBeat("amb_water_temvillage", "room == room_water_temvillage",
    "Temmie Village, a snug burrow full of excitable little cat-dog creatures who all chatter at once. There is a shop here, a save point, and a Temmie who will - for the right price - sell you the chance to pay for her college.");
beats += AmbBeat("amb_water17", "room == room_water17",
    "A pitch-dark room. A lantern can be picked up and carried to light a small circle around you; glowing stones and other lanterns help mark the safe way through.");
beats += AmbBeat("amb_water18", "room == room_water18",
    "A darkened path with tall grass and shallow water. Ahead, the armoured figure smashes clean through a wall of blocks in furious pursuit.");
beats += AmbBeat("amb_water19", "room == room_water19",
    "A tall, glowing shaft lined with whispering echo flowers, a save point resting partway up. The flowers here carry an uneasy warning.");
beats += AmbBeat("amb_water20", "room == room_water20",
    "A short, dim passage leading onward through the caverns.");
beats += AmbBeat("amb_water21", "room == room_water21",
    "A small puzzle room, a switch box set into the wall.");
beats += AmbBeat("amb_water13", "room == room_water13",
    "Another bridge-seed puzzle, set amid a patch of tall grass. Push the drifting seeds together to sprout lily-pad bridges across the water.");
beats += AmbBeat("amb_water_mushroom", "room == room_water_mushroom",
    "A small, quiet room with a sign to read and a curious mushroom-like monster.");
beats += AmbBeat("amb_water_undynefinal", "room == room_water_undynefinal",
    "A lonely cliff-top bathed in golden light at the edge of Waterfall. The armoured figure makes her stand here, and there is nowhere left to run - only to turn and face her.");
beats += AmbBeat("amb_water_undynefinal2", "room == room_water_undynefinal2",
    "A path fleeing toward a wall of rising heat, the armoured figure giving relentless chase.");
beats += AmbBeat("amb_water_undynefinal3", "room == room_water_undynefinal3",
    "The border between Waterfall and Hotland, marked by a sign. The air here turns suddenly, punishingly hot.");
// ---- Hotland: red rock over lava, Alphys's lab, MTT Resort (verified against _rooms_all.txt) ----
beats += AmbBeat("amb_fire1", "room == room_fire1",
    "You arrive in Hotland. The air is a wall of dry heat, the rock glows red, and far below churns a river of lava. A sentry station stands nearby with its watchman fast asleep, and a water cooler offers a cup of cold water.");
beats += AmbBeat("amb_fire2", "room == room_fire2",
    "A hot, narrow ledge above the lava. A water cooler stands here, and a chatty clam-like monster lingers by the path.");
beats += AmbBeat("amb_fire_prelab", "room == room_fire_prelab",
    "A ledge before a large grey laboratory built into the rock, its door sealed shut. A save point glows nearby.");
beats += AmbBeat("amb_fire_dock", "room == room_fire_dock",
    "A small dock at the lava's edge, where the hooded boatman waits to carry you onward if you wish.");
beats += AmbBeat("amb_fire_lab1", "room == room_fire_lab1",
    "The dim interior of the Royal Scientist's laboratory. A gigantic monitor dominates one wall, and the room is cluttered with instruments, a fridge, and bags of dog food. It is unsettlingly quiet.");
beats += AmbBeat("amb_fire_lab2", "room == room_fire_lab2",
    "A lower floor of the lab, escalators humming at either side. The shelves and cabinets here are packed with the scientist's not-so-secret collection of cartoons and figurines.");
beats += AmbBeat("amb_fire3", "room == room_fire3",
    "A hot junction of red rock, pipes running along the ground and warning notices posted about.");
beats += AmbBeat("amb_fire5", "room == room_fire5",
    "A tall shaft criss-crossed by moving conveyor belts, with jets of blue steam that puff you across the gaps. Ride the belts and the vents to climb it. A small volcano-like monster wanders here.");
beats += AmbBeat("amb_fire6", "room == room_fire6",
    "A room of steam vents. Stepping onto a vent launches you the way it blows; chain the jets together to cross the lava to the far side. A save point glows nearby.");
beats += AmbBeat("amb_fire6a", "room == room_fire6A",
    "A smaller room of conveyor belts and a steam vent, a fiery bird-like monster hovering nearby.");
beats += AmbBeat("amb_fire_lasers1", "room == room_fire_lasers1",
    "A laser puzzle. Beams of light bar the way: orange beams you must walk through while moving, blue beams only hurt if you move, so stand still to pass them. A switch at the end flips them all on or off.");
beats += AmbBeat("amb_fire7", "room == room_fire7",
    "A steam-vent room with a locked chip-card door in the middle. Ride the jets of steam across to reach the far side.");
beats += AmbBeat("amb_fire8", "room == room_fire8",
    "A hot room lit by a tall beacon tower, gears turning in the walls and a blue laser barring part of the path.");
beats += AmbBeat("amb_fire9", "room == room_fire9",
    "A small hot room, a beacon tower glowing at its top and gears set into the walls.");
beats += AmbBeat("amb_fire_shootguy_1", "room == room_fire_shootguy_1",
    "A shooting puzzle. A cannon sits at the bottom of the room and orange boxes float above; line the cannon up and fire to blast the boxes out of the way and clear the path.");
beats += AmbBeat("amb_fire_shootguy_2", "room == room_fire_shootguy_2",
    "Another shooting puzzle - aim the cannon and fire to knock the floating orange boxes aside.");
beats += AmbBeat("amb_fire_shootguy_3", "room == room_fire_shootguy_3",
    "A larger shooting puzzle, more orange boxes to blast clear with the cannon before you can pass.");
beats += AmbBeat("amb_fire_shootguy_4", "room == room_fire_shootguy_4",
    "A shooting puzzle - line up the cannon and fire to clear the floating orange boxes from your path.");
beats += AmbBeat("amb_fire_shootguy_5", "room == room_fire_shootguy_5",
    "A shooting puzzle set within the CORE, orange boxes to blast aside with the cannon.");
beats += AmbBeat("amb_fire_turn", "room == room_fire_turn",
    "A hot corner where jets of steam bounce you around the bend.");
beats += AmbBeat("amb_fire4", "room == room_fire4",
    "A large steam-vent room, jets launching you rightward from ledge to ledge across the lava.");
beats += AmbBeat("amb_fire_cookingshow", "room == room_fire_cookingshow",
    "A brightly-lit cooking-show stage, complete with a counter and dazzling studio lights. A robotic television star is putting on a live cooking programme here.");
beats += AmbBeat("amb_fire_savepoint1", "room == room_fire_savepoint1",
    "A ledge with a save point and a sweeping view of the CORE, the great glowing engine of the Underground, rising in the distance.");
beats += AmbBeat("amb_fire_hotdog", "room == room_fire_hotdog",
    "A sentry station where a certain lazy skeleton sells hot dogs, a warm and greasy smell hanging in the air.");
beats += AmbBeat("amb_fire_walkandbranch", "room == room_fire_walkandbranch",
    "A hot branching path over the lava, warning notices posted along it and an aeroplane-like monster drifting nearby.");
beats += AmbBeat("amb_fire_sorry", "room == room_fire_sorry",
    "A small out-of-the-way room with an art-class sign on the wall.");
beats += AmbBeat("amb_fire_apron", "room == room_fire_apron",
    "A hot ledge where a wave-making monster lingers by the path.");
beats += AmbBeat("amb_fire10", "room == room_fire10",
    "A conveyor-belt room with a row of three switches to set in the right pattern before the way opens.");
beats += AmbBeat("amb_fire_rpuzzle", "room == room_fire_rpuzzle",
    "A puzzle of steam vents and conveyor belts. Bounce between the jets and ride the belts to shift the blocks and cross.");
beats += AmbBeat("amb_fire_mewmew2", "room == room_fire_mewmew2",
    "A hot room with a save point and a little mouse hole worn in the wall.");
beats += AmbBeat("amb_fire_boysnightout", "room == room_fire_boysnightout",
    "A hot room of steam vents, a fiery bird-like monster hanging about among the jets.");
beats += AmbBeat("amb_fire_newsreport", "room == room_fire_newsreport",
    "A studio set where the robotic television star is filming a live news report, lasers and steam vents rigged around the stage.");
beats += AmbBeat("amb_fire_coreview2", "room == room_fire_coreview2",
    "A ledge with another grand view of the CORE glowing in the distance.");
beats += AmbBeat("amb_fire_spidershop", "room == room_fire_spidershop",
    "A cosy corner strung with spider webs, where a cheerful spider-girl runs a bake sale. Leave a few coins and the spiders will sell you a treat - all made for spiders, by spiders.");
beats += AmbBeat("amb_fire_walkandbranch2", "room == room_fire_walkandbranch2",
    "A large steam-vent maze climbing upward, jets of steam launching you from one ledge to the next.");
beats += AmbBeat("amb_fire_conveyorlaser", "room == room_fire_conveyorlaser",
    "A room combining moving conveyor belts with blue laser beams - stay perfectly still through the blue beams while the belts carry you along. An echo flower glows to one side.");
beats += AmbBeat("amb_fire_preshootguy4", "room == room_fire_preshootguy4",
    "A hot little room lit by a beacon tower, a couple of gem-like child monsters loitering here.");
beats += AmbBeat("amb_fire_savepoint2", "room == room_fire_savepoint2",
    "A hot ledge strung with spider silk, a save point glowing softly here.");
beats += AmbBeat("amb_fire_spider", "room == room_fire_spider",
    "A long, web-draped corridor deep in spider territory. Sticky silk slows your steps, spiders watch from above, and their mistress is not far off.");
beats += AmbBeat("amb_fire_pacing", "room == room_fire_pacing",
    "A small room with a sign to read and a monster pacing about.");
beats += AmbBeat("amb_fire_multitile", "room == room_fire_multitile",
    "A huge coloured-tile puzzle: a long floor of tiles in many colours, each colour carrying its own rule - some safe, some not - which a voice calls out before you cross. A little volcano-monster waits nearby.");
beats += AmbBeat("amb_fire_hotelfront_1", "room == room_fire_hotelfront_1",
    "The grand entrance to the MTT Resort, a glitzy hotel carved out over the lava. The Nice Cream vendor has set up his cart by the doors.");
beats += AmbBeat("amb_fire_hotelfront_2", "room == room_fire_hotelfront_2",
    "The approach to the hotel doors, plush red carpet underfoot.");
beats += AmbBeat("amb_fire_hotellobby", "room == room_fire_hotellobby",
    "The opulent lobby of the MTT Resort, gaudy and gold. A fountain shaped like the robotic star bubbles in the centre, a receptionist waits at the desk, and well-dressed monster guests mill about. A save point glows nearby, and an elevator stands ready.");
beats += AmbBeat("amb_fire_restaurant", "room == room_fire_restaurant",
    "The hotel's fancy restaurant, tables set with care and potted plants along the walls, guests dining quietly.");
beats += AmbBeat("amb_fire_hoteldoors", "room == room_fire_hoteldoors",
    "A hallway of guest-room doors, a weary slime janitor mopping the floor.");
beats += AmbBeat("amb_fire_hotelbed", "room == room_fire_hotelbed",
    "A plush hotel bedroom, a large soft bed waiting - a rest here will restore you.");
beats += AmbBeat("amb_fire_precore", "room == room_fire_precore",
    "A dark shaft leading down toward the CORE, the hum of heavy machinery rising from below.");
beats += AmbBeat("amb_fire_core1", "room == room_fire_core1",
    "The entrance to the CORE, the Underground's vast power plant. Dark metal walls glow with strips of blue light, the air thrumming with energy. An elevator stands here.");
beats += AmbBeat("amb_fire_core2", "room == room_fire_core2",
    "A CORE chamber lit an eerie blue, small flames flickering in braziers along the walls.");
beats += AmbBeat("amb_fire_core3", "room == room_fire_core3",
    "A CORE room lined with glowing totems, a great door set in the wall ahead.");
beats += AmbBeat("amb_fire_core4", "room == room_fire_core4",
    "A CORE room barred by laser beams, a switch nearby to toggle them - walk through the orange, stand still for the blue.");
beats += AmbBeat("amb_fire_core5", "room == room_fire_core5",
    "A small CORE chamber glowing with totems and blue light.");
beats += AmbBeat("amb_fire_core_freebattle", "room == room_fire_core_freebattle",
    "A small CORE chamber where a shadowy foe lurks in the blue light.");
beats += AmbBeat("amb_fire_core_laserfun", "room == room_fire_core_laserfun",
    "A long CORE hall crossed by a gauntlet of blue and orange laser beams. Walk through the orange, freeze still for the blue.");
beats += AmbBeat("amb_fire_core_branch", "room == room_fire_core_branch",
    "A CORE junction with a save point and signs, glowing light-strips marking several ways onward. It is easy to get turned around in here.");
beats += AmbBeat("amb_fire_core_bottomleft", "room == room_fire_core_bottomleft",
    "A CORE corridor with conveyor belts running along the floor and blue light-strips glowing on the dark walls.");
beats += AmbBeat("amb_fire_core_left", "room == room_fire_core_left",
    "A CORE corridor branching off to the left, glowing totems set in the walls.");
beats += AmbBeat("amb_fire_core_topleft", "room == room_fire_core_topleft",
    "A CORE corridor junction, blue light-strips lining the dark metal walls.");
beats += AmbBeat("amb_fire_core_top", "room == room_fire_core_top",
    "A CORE corridor near the top of the maze, signs posted and light-strips glowing.");
beats += AmbBeat("amb_fire_core_topright", "room == room_fire_core_topright",
    "A CORE corridor junction humming with power, blue light running along the walls.");
beats += AmbBeat("amb_fire_core_right", "room == room_fire_core_right",
    "A CORE corridor where a shimmering force-field seals one of the doors.");
beats += AmbBeat("amb_fire_core_bottomright", "room == room_fire_core_bottomright",
    "A CORE corner where a walkway bridges the dark drop, glowing totems standing guard.");
beats += AmbBeat("amb_fire_core_center", "room == room_fire_core_center",
    "The central CORE junction, paths branching in every direction, a shadowy foe prowling nearby.");
beats += AmbBeat("amb_fire_core_treasureleft", "room == room_fire_core_treasureleft",
    "A CORE alcove off the maze, holding something worth taking and a monster to talk to.");
beats += AmbBeat("amb_fire_core_treasureright", "room == room_fire_core_treasureright",
    "A CORE alcove tucked away off the maze, holding a small reward.");
beats += AmbBeat("amb_fire_core_warrior", "room == room_fire_core_warrior",
    "A CORE hall where a fierce warrior monster blocks the way, a switch glinting at the far end.");
beats += AmbBeat("amb_fire_core_bridge", "room == room_fire_core_bridge",
    "A long CORE bridge spanning a dark chasm, glowing totems lining the rails and light streaming overhead.");
beats += AmbBeat("amb_fire_core_premett", "room == room_fire_core_premett",
    "A CORE chamber with a save point and a great door ahead, an elevator waiting to one side.");
beats += AmbBeat("amb_fire_core_metttest", "room == room_fire_core_metttest",
    "A tall, dramatic CORE stage - the setting for a grand confrontation with the robotic star.");
beats += AmbBeat("amb_fire_core_final", "room == room_fire_core_final",
    "The far end of the CORE, an elevator waiting to carry you up out of the depths.");
beats += AmbBeat("amb_fire_elevator", "room == room_fire_elevator || room == room_fire_finalelevator || room == room_fire_labelevator",
    "A small elevator car, its control panel glowing beside the door.");
beats += AmbBeat("amb_fire_elevator_gems", "room == room_fire_elevator_r1 || room == room_fire_elevator_r2 || room == room_fire_elevator_r3 || room == room_fire_elevator_l1 || room == room_fire_elevator_l2 || room == room_fire_elevator_l3",
    "A small elevator car, the floor number glowing on a sign by the door.");
// ---- The True Lab: dark and half-forgotten beneath the lab (verified against _rooms_all.txt) ----
beats += AmbBeat("amb_truelab_elevatorinside", "room == room_truelab_elevatorinside",
    "A dim elevator that has carried you somewhere you were never meant to go.");
beats += AmbBeat("amb_truelab_elevator", "room == room_truelab_elevator",
    "A dark landing outside the elevator, a heavy lab door standing ahead.");
beats += AmbBeat("amb_truelab_hall1", "room == room_truelab_hall1",
    "A dark laboratory hall, its walls lined with humming monitors that cast a sickly glow.");
beats += AmbBeat("amb_truelab_hub", "room == room_truelab_hub",
    "A shadowy hub where several lab doors meet. A save point glows here, dark withered plants stand in their pots, and a torn note lies on the floor.");
beats += AmbBeat("amb_truelab_hall2", "room == room_truelab_hall2",
    "A short, dark lab corridor, a monitor flickering on the wall.");
beats += AmbBeat("amb_truelab_operatingroom", "room == room_truelab_operatingroom",
    "A grim operating room half-lost in fog, empty tables and grimy sinks lined up in the gloom.");
beats += AmbBeat("amb_truelab_redlever", "room == room_truelab_redlever",
    "A dark room with a coloured lever on the wall - one of several to be set - and a torn note lying nearby.");
beats += AmbBeat("amb_truelab_bluelever", "room == room_truelab_bluelever",
    "A dark room with a coloured lever on the wall and a torn note on the floor.");
beats += AmbBeat("amb_truelab_greenlever", "room == room_truelab_greenlever",
    "A dark room with a coloured lever and a torn note lying beside it.");
beats += AmbBeat("amb_truelab_prebed", "room == room_truelab_prebed",
    "A dark corridor lined with glowing monitors.");
beats += AmbBeat("amb_truelab_bedroom", "room == room_truelab_bedroom",
    "A dim dormitory of empty beds shrouded in fog. Something watches from among them. A save point glows in one corner, and a key rests on one of the beds.");
beats += AmbBeat("amb_truelab_mirror", "room == room_truelab_mirror",
    "A long dark hall lined with mirrors. In the fog at the far end, a strange, shifting shape waits and watches.");
beats += AmbBeat("amb_truelab_hall3", "room == room_truelab_hall3",
    "Another dark lab corridor, monitors glowing faintly along the walls.");
beats += AmbBeat("amb_truelab_shower", "room == room_truelab_shower",
    "A small room with a drawn shower curtain, something lurking behind it.");
beats += AmbBeat("amb_truelab_determination", "room == room_truelab_determination",
    "A dark room built around a strange extraction machine. A save point glows here - though this one seems oddly, unsettlingly alive.");
beats += AmbBeat("amb_truelab_tv", "room == room_truelab_tv",
    "A dark room with an old television set and a coloured lever, a torn note on the floor.");
beats += AmbBeat("amb_truelab_cooler", "room == room_truelab_cooler",
    "A cold storage room, ranks of dark fridges humming beneath whirring fans. Something stirs among them.");
beats += AmbBeat("amb_truelab_fan", "room == room_truelab_fan",
    "A room walled with great spinning fans, fog curling between them. A shape moves in the mist.");
beats += AmbBeat("amb_truelab_prepower", "room == room_truelab_prepower",
    "A dark corridor of monitors leading toward the power room.");
beats += AmbBeat("amb_truelab_power", "room == room_truelab_power",
    "A small room holding the main power switch, ready to bring the lights back on.");
beats += AmbBeat("amb_truelab_castle_elevator", "room == room_truelab_castle_elevator",
    "An elevator ready to carry you up out of the true lab.");
// ---- New Home: the King's grey city, Asgore's house, the castle (verified against _rooms_all.txt) ----
beats += AmbBeat("amb_castle_elevatorout", "room == room_castle_elevatorout",
    "You step out of the elevator into New Home, the King's grey city high above. A save point glows nearby, and a slow, mournful music hangs in the air.");
beats += AmbBeat("amb_castle_precastle", "room == room_castle_precastle",
    "A long grey approach toward the castle, the ruins of an old, old home looming quietly ahead.");
beats += AmbBeat("amb_castle_front", "room == room_castle_front",
    "The front of the castle, tall and grey and silent. A save point glows here.");
beats += AmbBeat("amb_kitchen", "room == room_kitchen || room == room_kitchen_final",
    "A tidy little kitchen. A freshly baked pie rests on the counter, filling the still air with a sweet and sorrowful smell.");
beats += AmbBeat("amb_asghouse1", "room == room_asghouse1",
    "You step into a home almost exactly like Toriel's - the same shape, the same rooms - but grey, hushed, and long abandoned, everything left just as it was. Stairs lead down from the entrance hall.");
beats += AmbBeat("amb_asghouse2", "room == room_asghouse2",
    "A living room that echoes Toriel's exactly: a reading chair beside a cold hearth, a dining table set for a family. But it is dim and empty now, coated in quiet dust.");
beats += AmbBeat("amb_asghouse3", "room == room_asghouse3",
    "A hallway like the one in Toriel's home, a mirror hanging on the wall. The bedrooms off this hall hold the belongings of children who are long gone.");
beats += AmbBeat("amb_lastruins_corridor", "room == room_lastruins_corridor",
    "A long grey corridor lined with plaques. Each one, as you pass, tells another piece of the Underground's sad and ancient history.");
beats += AmbBeat("amb_sanscorridor", "room == room_sanscorridor",
    "A vast hall of golden light, tall pillars throwing long shadows and sunlight streaming through great windows. A save point glows near the entrance, and far off at the other end, a figure waits to weigh the journey you have made.");
beats += AmbBeat("amb_castle_finalshoehorn", "room == room_castle_finalshoehorn",
    "A quiet grey chamber with a save point, the way narrowing toward the throne room ahead.");
beats += AmbBeat("amb_castle_coffins2", "room == room_castle_coffins2",
    "A solemn room lined with child-sized coffins, each one carefully made and marked with a name.");
beats += AmbBeat("amb_castle_throneroom", "room == room_castle_throneroom",
    "The throne room, carpeted with golden flowers that glow in the light pouring from above. Two thrones stand here, one of them draped and unused. This is where the King waits. A save point glows nearby.");
beats += AmbBeat("amb_castle_prebarrier", "room == room_castle_prebarrier",
    "A solemn grey chamber just before the barrier, a save point glowing softly here.");
beats += AmbBeat("amb_castle_barrier", "room == room_castle_barrier",
    "The barrier itself: a towering wall of blinding white light, the ancient magic that seals the whole Underground away from the world above.");
beats += AmbBeat("amb_castle_trueexit", "room == room_castle_trueexit",
    "A passage leading up and onward, toward the surface at last.");

string gml = @"
{
    if (variable_global_exists(""nvda_opt_speech"") && global.nvda_opt_speech == 0) exit;
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }
    if (!variable_global_exists(""nvda_speak_queue"")) global.nvda_speak_queue = external_define(""nvda_gm.dll"", ""gmnvda_speak_queue"", 0, 0, 1, 1);
    if (!variable_global_exists(""nvda_cs_seen"")) global.nvda_cs_seen = ""|"";
    if (!variable_global_exists(""nvda_now"")) global.nvda_now = 0;
    global.nvda_now += 1;   // frame counter for the priority window (obj_time Step = once/frame)
    if (!variable_global_exists(""nvda_room_prev"")) global.nvda_room_prev = -1;
    global.nvda_roomchg = (global.nvda_room_prev != room);   // did we just change rooms this frame?
    global.nvda_room_prev = room;
    if (!variable_global_exists(""nvda_roomdesc"")) global.nvda_roomdesc = """";
    if (global.nvda_roomchg) global.nvda_roomdesc = """";   // stale-clear; the matching AmbBeat re-sets it
" + beats + @"
    // L key: re-speak the current room's environmental description on demand (overworld only).
    if (instance_exists(obj_mainchara) && keyboard_check_pressed(ord(""L"")))
    {
        if (global.nvda_roomdesc != """") external_call(global.nvda_speak, global.nvda_roomdesc);
        else external_call(global.nvda_speak, ""There is no description for this area."");
    }
}
";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_time_Step_1"), gml);
g.Import();
Console.WriteLine("Injected cutscene audio descriptions (intro beats)");

}

// ===== inject_naming.csx =====
{
// Undertale accessibility - naming screen ("name the fallen human").
// Appends a per-frame announcer to scr_namingscreen.  Speaks:
//  - the letter under the cursor as it moves (or Quit/Backspace/Done on bottom row)
//  - the name so far as letters are added/removed
//  - the confirm prompt + No/Yes on the "is this correct?" screen
// Tracking vars live on the host instance (obj_intromenu).

Data.Functions.EnsureDefined("external_define", Data.Strings);
Data.Functions.EnsureDefined("external_call", Data.Strings);
Data.Functions.EnsureDefined("variable_global_exists", Data.Strings);
Data.Functions.EnsureDefined("variable_instance_exists", Data.Strings);

string gml = @"
{
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }
    if (!variable_instance_exists(id, ""nvda_nm_init""))
    {
        nvda_nm_init = 1;
        nvda_last_naming = -99;
        nvda_last_row = -99;
        nvda_last_col = -99;
        nvda_last_name = ""<x>"";
        nvda_last_sel2 = -99;
        nvda_last_specm = ""<x>"";
        nvda_last_sel3 = -99;
    }
    // New Game in progress (name entry, naming 1/2) -> reset the character-description
    // seen-set so a fresh playthrough re-introduces everyone. Continue uses naming==3 and
    // never touches this. In-memory only (no ini write at the title - the game holds
    // undertale.ini open there); the cleared set is flushed once back in gameplay.
    if ((naming == 1 || naming == 2) && variable_global_exists(""nvda_seen_chars"") && global.nvda_seen_chars != ""|"")
        global.nvda_seen_chars = ""|"";
    if ((naming == 1 || naming == 2) && variable_global_exists(""nvda_cs_seen"") && global.nvda_cs_seen != ""|"")
        global.nvda_cs_seen = ""|"";
    if ((naming == 1 || naming == 2) && variable_global_exists(""nvda_amb_seen"") && global.nvda_amb_seen != ""|"")
        global.nvda_amb_seen = ""|"";
    var _say = """";
    if (naming != nvda_last_naming)
    {
        nvda_last_naming = naming;
        nvda_last_row = selected_row;
        nvda_last_col = selected_col;
        nvda_last_name = charname;
        nvda_last_specm = ""<x>"";
        nvda_last_sel2 = -99;
        nvda_last_sel3 = selected3;
        if (naming == 1)
            _say = ""Name entry. Arrow keys move, Z selects a letter, X deletes. Spell a name, then choose Done."";
        else if (naming == 3)
        {
            if (hasname == 1)
            {
                var _o3 = ""Continue"";
                if (selected3 == 1) { if (truereset == 0) _o3 = ""Reset""; else _o3 = ""True Reset""; }
                else if (selected3 == 2) _o3 = ""Settings"";
                _say = ""Load menu. Left or right for Continue or Reset, down for Settings and Accessibility options. On "" + _o3;
            }
            else
            {
                var _b3 = ""Begin Game"";
                if (selected3 == 1) _b3 = ""Settings"";
                _say = ""Title menu. Up or down to choose, Z to confirm. On "" + _b3;
            }
        }
    }
    if (naming == 1)
    {
        if (selected_row != nvda_last_row || selected_col != nvda_last_col)
        {
            nvda_last_row = selected_row;
            nvda_last_col = selected_col;
            if (selected_row == -1)
            {
                if (selected_col == 0) _say = ""Quit"";
                else if (selected_col == 1) _say = ""Backspace"";
                else if (selected_col == 2) _say = ""Done"";
            }
            else if (selected_row == -2)
            {
                _say = ""Character set"";
            }
            else
            {
                var _ch = charmap[selected_row, selected_col];
                if (_ch == """") _say = ""blank""; else _say = _ch;
            }
        }
        if (charname != nvda_last_name)
        {
            nvda_last_name = charname;
            if (charname == """") _say = ""name empty""; else _say = ""Name "" + charname;
        }
    }
    else if (naming == 2)
    {
        // spec_m / allow / selected2 are only created by the game's own naming==2
        // block, which is skipped on the frame naming flips to 2 (from the grid's
        // ""Done"" at line 465 or the Reset path at line 645).  Reading spec_m before
        // the game sets it = fatal ""not set before reading it"".  Wait one frame.
        if (variable_instance_exists(id, ""spec_m"") && variable_instance_exists(id, ""allow"") && variable_instance_exists(id, ""selected2""))
        {
            if (spec_m != nvda_last_specm)
            {
                nvda_last_specm = spec_m;
                nvda_last_sel2 = selected2;
                var _opt = ""Go back"";
                if (allow) { if (selected2 == 1) _opt = ""Yes""; else _opt = ""No""; }
                _say = spec_m + "". "" + _opt;
            }
            else if (selected2 != nvda_last_sel2)
            {
                nvda_last_sel2 = selected2;
                if (allow) { if (selected2 == 1) _say = ""Yes""; else _say = ""No""; }
                else _say = ""Go back"";
            }
        }
    }
    else if (naming == 3)
    {
        if (selected3 != nvda_last_sel3)
        {
            nvda_last_sel3 = selected3;
            if (hasname == 1)
            {
                if (selected3 == 0) _say = ""Continue"";
                else if (selected3 == 1) { if (truereset == 0) _say = ""Reset""; else _say = ""True Reset""; }
                else if (selected3 == 2) _say = ""Settings"";
                else if (selected3 == 3) _say = ""Accessibility options"";
            }
            else
            {
                if (selected3 == 0) _say = ""Begin Game"";
                else if (selected3 == 1) _say = ""Settings"";
                else if (selected3 == 2) _say = ""Accessibility options"";
            }
        }
    }
    // Open the accessibility menu when Z is confirmed on the new 4th item. The game set
    // local 'action' = selected3 on confirm but has no handler for our extra index, so
    // we open our own menu (driven by obj_time Step_1). Guard on naming==3 so 'action'
    // was assigned this frame. accidx = 3 with a save (Continue/Reset/Settings/Access),
    // else 2 (Begin Game/Settings/Access).
    if (naming == 3)
    {
        var _accidx = 3;
        if (hasname != 1) _accidx = 2;
        if (action == _accidx && global.nvda_menu_open == 0)
        {
            global.nvda_menu_open = 1;
            global.nvda_menu_sel = 0;
        }
    }
    if (_say != """")
        external_call(global.nvda_speak, _say);
}";

var importGroup = new UndertaleModLib.Compiler.CodeImportGroup(Data);
importGroup.QueueAppend(Data.Code.ByName("gml_Script_scr_namingscreen"), gml);
importGroup.Import();
Console.WriteLine("Injected NVDA announcer into scr_namingscreen");

}

// ===== inject_menus.csx =====
{
// Undertale accessibility - title logo splash (obj_titleimage).
// Speaks a prompt on entry so a blind player knows to press confirm to proceed
// to the load menu.

Data.Functions.EnsureDefined("external_define", Data.Strings);
Data.Functions.EnsureDefined("external_call", Data.Strings);
Data.Functions.EnsureDefined("variable_global_exists", Data.Strings);

string gml = @"
{
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }
    external_call(global.nvda_speak, ""Undertale. Press the confirm button, Z or Enter, to start."");
}";

var importGroup = new UndertaleModLib.Compiler.CodeImportGroup(Data);
importGroup.QueueAppend(Data.Code.ByName("gml_Object_obj_titleimage_Create_0"), gml);
importGroup.Import();
Console.WriteLine("Injected NVDA into obj_titleimage Create (title splash)");

}

// ===== inject_settings.csx =====
{
// Undertale accessibility - settings menu (obj_settingsmenu).
// Per-frame announcer appended to its Draw: speaks the focused option as the
// cursor (menu) moves, the language value, and a hint when binding a button.
// PC option order (menu 0..6): Exit, Language, Confirm button, Cancel button,
// Menu button, Reset controls, Border.

Data.Functions.EnsureDefined("external_define", Data.Strings);
Data.Functions.EnsureDefined("external_call", Data.Strings);
Data.Functions.EnsureDefined("variable_global_exists", Data.Strings);
Data.Functions.EnsureDefined("variable_instance_exists", Data.Strings);

string gml = @"
{
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }
    if (!variable_instance_exists(id, ""nvda_set_init""))
    {
        nvda_set_init = 1;
        nvda_last_menu = -99;
        nvda_last_engage = -99;
        nvda_last_lang = """";
        nvda_entered = 0;
    }
    var _langname = ""Japanese"";
    if (global.language == ""en"") _langname = ""English"";
    var _lbl = """";
    if (menu == 0) _lbl = ""Exit"";
    else if (menu == 1) _lbl = ""Language, "" + _langname;
    else if (menu == 2) _lbl = ""Confirm button"";
    else if (menu == 3) _lbl = ""Cancel button"";
    else if (menu == 4) _lbl = ""Menu button"";
    else if (menu == 5) _lbl = ""Reset controls"";
    else if (menu == 6) _lbl = ""Border"";

    var _say = """";
    if (nvda_entered == 0)
    {
        nvda_entered = 1;
        nvda_last_menu = menu;
        nvda_last_lang = global.language;
        nvda_last_engage = menu_engage;
        _say = ""Settings. Up and down to move, Z to select, left and right to change a value. On "" + _lbl;
    }
    else
    {
        if (menu != nvda_last_menu)
        {
            nvda_last_menu = menu;
            nvda_last_lang = global.language;
            _say = _lbl;
        }
        if (global.language != nvda_last_lang)
        {
            nvda_last_lang = global.language;
            _say = ""Language, "" + _langname;
        }
        if (menu_engage != nvda_last_engage)
        {
            nvda_last_engage = menu_engage;
            if (menu_engage == 1 && menu >= 2 && menu <= 4)
                _say = ""Press a key to bind it."";
        }
    }
    if (_say != """")
        external_call(global.nvda_speak, _say);
}";

var importGroup = new UndertaleModLib.Compiler.CodeImportGroup(Data);
importGroup.QueueAppend(Data.Code.ByName("gml_Object_obj_settingsmenu_Draw_0"), gml);
importGroup.Import();
Console.WriteLine("Injected NVDA announcer into obj_settingsmenu Draw");

}

// ===== inject_nav.csx =====
{
// Undertale accessibility - navigation v4 (unified interactable cursor, Stardew/Skyrim style).
//  Create: announce room name on entry, reset cursor.
//  Step_0 (always):
//    - T = next interactable (nearest outward); R = previous.
//          announces kind + name + dir + dist (+ on/off for switches); sets live guide target.
//    - E = summary: count of interactables + nearest one.
//    - G = repeat current target.
//    - Y = diagnostic: raw object names within ~80px.
//  Step_0 (while moving):
//    - live guidance to the selected interactable (dir+dist on change, "reached").
//    - "Blocked" when pushing into a wall (throttled).
//  Unified list = with(obj_interactable) [all switches/signs/npcs/items/saves] + with(obj_doorparent) [exits].
//  Read-only awareness; player walks manually.

string[] need = { "external_define","external_call","variable_global_exists","variable_instance_exists",
                  "keyboard_check","keyboard_check_pressed","ord","point_distance","instance_nearest",
                  "instance_exists","is_string","scr_roomname","round","string","object_get_name",
                  "ds_list_create","ds_list_add","ds_list_size","ds_list_find_value",
                  "ds_list_replace","ds_list_destroy","ds_list_delete","string_copy","string_length","string_replace_all" };
foreach (var f in need) Data.Functions.EnsureDefined(f, Data.Strings);

string bridge = @"
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }
    if (!variable_global_exists(""nvda_idx"")) global.nvda_idx = -1;
    if (!variable_global_exists(""nvda_listn"")) global.nvda_listn = 0;
    if (!variable_global_exists(""nvda_sel"")) global.nvda_sel = noone;";

// Tag every interactable + door with a friendly kind (general -> specific, later wins).
string tag = @"
    with (obj_interactable) nvda_kind = ""Object"";
    with (obj_wanderparent) nvda_kind = ""Character"";
    with (obj_readable) nvda_kind = ""Sign"";
    with (obj_switch) nvda_kind = ""Switch"";
    with (obj_switchbasic) nvda_kind = ""Switch"";
    with (obj_readable_switch1) nvda_kind = ""Switch"";
    with (obj_redswitch_1) nvda_kind = ""Switch"";
    with (obj_savepoint) nvda_kind = ""Save point"";
    with (obj_pushrock1) nvda_kind = ""Rock"";
    with (obj_floorswitch1) nvda_kind = ""Floor switch"";
    with (obj_spiketile1) nvda_kind = ""Spikes"";
    with (obj_spiketile2) nvda_kind = ""Spikes"";
    with (obj_spikes_room) nvda_kind = ""Spikes"";
    with (obj_holedown) nvda_kind = ""Hole"";
    with (obj_superdrophole) nvda_kind = ""Hole"";
    with (obj_holeup) nvda_kind = ""Way up"";
    with (obj_holeup2) nvda_kind = ""Way up"";
    with (obj_xoxo) { if (image_index == 0) nvda_kind = ""X, step on it""; else if (image_index == 1) nvda_kind = ""O, done, avoid""; else nvda_kind = ""O tile""; }
    with (obj_xoxocontroller1) nvda_kind = ""Switch, press when all are O"";
    with (obj_doorparent) nvda_kind = ""Exit"";";

// Build global.nvda_list[] sorted by distance to the player (selection sort; rooms are small).
string buildList = @"
    var _list = ds_list_create();
    with (obj_interactable) ds_list_add(_list, id);
    // doors, but NOT the ice-slide trigger tiles (obj_iceevent/up/right are obj_doorparent
    // children; a slide room has 100+ of them -> they flooded the scanner as fake ""exits"").
    with (obj_doorparent)
    {
        if (object_index != obj_iceevent && object_index != obj_iceeventup && object_index != obj_iceeventright)
            ds_list_add(_list, id);
    }
    with (obj_pushrock1) ds_list_add(_list, id);
    with (obj_floorswitch1) ds_list_add(_list, id);
    with (obj_spiketile1) ds_list_add(_list, id);
    with (obj_spiketile2) ds_list_add(_list, id);
    with (obj_spikes_room) ds_list_add(_list, id);
    with (obj_holedown) ds_list_add(_list, id);
    with (obj_superdrophole) ds_list_add(_list, id);
    with (obj_holeup) ds_list_add(_list, id);
    with (obj_holeup2) ds_list_add(_list, id);
    with (obj_xoxo) ds_list_add(_list, id);
    with (obj_xoxocontroller1) ds_list_add(_list, id);
    // DEDUPE: a single door/sign is often several adjacent instances of the same object. Drop any
    // instance that shares its object_index with an earlier-kept one within 48px (= same logical
    // thing) so one wide door / multi-tile sign shows up ONCE instead of 4 times.
    var _dd = 0;
    for (_dd = ds_list_size(_list) - 1; _dd > 0; _dd -= 1)
    {
        var _da = ds_list_find_value(_list, _dd);
        var _de = 0;
        var _dup = 0;
        for (_de = 0; _de < _dd; _de += 1)
        {
            var _db = ds_list_find_value(_list, _de);
            if (_da.object_index == _db.object_index && point_distance(_da.x, _da.y, _db.x, _db.y) < 48)
            {
                _dup = 1;
                break;
            }
        }
        if (_dup == 1) ds_list_delete(_list, _dd);
    }
    var _n = ds_list_size(_list);
    var _px = x; var _py = y;
    var _i = 0;
    for (_i = 0; _i < _n - 1; _i += 1)
    {
        var _mn = _i;
        var _j = 0;
        for (_j = _i + 1; _j < _n; _j += 1)
        {
            var _ja = ds_list_find_value(_list, _j);
            var _mb = ds_list_find_value(_list, _mn);
            if (point_distance(_px, _py, _ja.x, _ja.y) < point_distance(_px, _py, _mb.x, _mb.y)) _mn = _j;
        }
        if (_mn != _i)
        {
            var _tmp = ds_list_find_value(_list, _i);
            ds_list_replace(_list, _i, ds_list_find_value(_list, _mn));
            ds_list_replace(_list, _mn, _tmp);
        }
    }
    global.nvda_listn = _n;
    var _k = 0;
    for (_k = 0; _k < _n; _k += 1) global.nvda_list[_k] = ds_list_find_value(_list, _k);
    ds_list_destroy(_list);";

// Given _sel (an instance id), build _out = full announcement string.
string announce = @"
    var _dx = _sel.x - x; var _dy = _sel.y - y;
    var _steps = round(point_distance(x, y, _sel.x, _sel.y) / 20);
    var _dir = """";
    if (_dy < -16) _dir = ""up""; else if (_dy > 16) _dir = ""down"";
    if (_dx < -16) { if (_dir != """") _dir += "" and ""; _dir += ""left""; } else if (_dx > 16) { if (_dir != """") _dir += "" and ""; _dir += ""right""; }
    if (_dir == """") _dir = ""here"";
    var _kind = ""Object"";
    if (variable_instance_exists(_sel, ""nvda_kind"")) _kind = _sel.nvda_kind;
    var _nm = object_get_name(_sel.object_index);
    if (string_length(_nm) > 4 && string_copy(_nm, 1, 4) == ""obj_"") _nm = string_copy(_nm, 5, string_length(_nm) - 4);
    _nm = string_replace_all(_nm, ""_"", "" "");
    var _state = """";
    if (_kind == ""Switch"" && variable_instance_exists(_sel, ""on"")) { if (_sel.on) _state = "", on""; else _state = "", off""; }
    var _out = _kind + "", "" + _nm + _state;
    if (_dir == ""here"") _out += "", here""; else _out += "", "" + _dir + "", "" + string(_steps) + "" steps"";";

// ---- Create: room name + reset cursor ----
string createGml = @"
{" + bridge + @"
    nvda_lastdir = ""<x>"";
    nvda_blocktimer = 0;
    nvda_holetimer = 0;
    nvda_startdelay = 15;
    global.nvda_idx = -1;
    global.nvda_sel = noone;
    var _rn = scr_roomname(room);
    if (is_string(_rn) && _rn != """")
        external_call(global.nvda_speak, _rn);
}";

// ---- Step_0 ----
string stepGml = @"
{
    if (variable_global_exists(""nvda_opt_scan"") && global.nvda_opt_scan == 0) { } else {" + bridge + @"
    if (!variable_instance_exists(id, ""nvda_lastdir""))
    {
        nvda_lastdir = ""<x>""; nvda_blocktimer = 0; nvda_startdelay = 0;
    }
    if (!variable_instance_exists(id, ""nvda_holetimer"")) nvda_holetimer = 0;

    // T = next interactable, R = previous (cycle).
    if (keyboard_check_pressed(ord(""T"")) || keyboard_check_pressed(ord(""R"")))
    {" + tag + buildList + @"
        if (global.nvda_listn <= 0)
        {
            external_call(global.nvda_speak, ""No interactables here."");
        }
        else
        {
            if (keyboard_check_pressed(ord(""R""))) global.nvda_idx -= 1; else global.nvda_idx += 1;
            if (global.nvda_idx >= global.nvda_listn) global.nvda_idx = 0;
            if (global.nvda_idx < 0) global.nvda_idx = global.nvda_listn - 1;
            global.nvda_sel = global.nvda_list[global.nvda_idx];
            var _sel = global.nvda_sel;" + announce + @"
            external_call(global.nvda_speak, _out + "". "" + string(global.nvda_idx + 1) + "" of "" + string(global.nvda_listn));
            nvda_lastdir = _dir;
        }
    }

    // E: summary - count + nearest interactable.
    if (keyboard_check_pressed(ord(""E"")))
    {" + tag + buildList + @"
        if (global.nvda_listn <= 0)
        {
            external_call(global.nvda_speak, ""Nothing interactive nearby."");
        }
        else
        {
            var _sel = global.nvda_list[0];" + announce + @"
            external_call(global.nvda_speak, string(global.nvda_listn) + "" interactables. Nearest: "" + _out);
        }
    }

    // G: repeat / re-announce current target.
    if (keyboard_check_pressed(ord(""G"")))
    {
        if (variable_global_exists(""nvda_sel"") && global.nvda_sel != noone && instance_exists(global.nvda_sel))
        {
            var _sel = global.nvda_sel;" + announce + @"
            external_call(global.nvda_speak, _out);
        }
        else external_call(global.nvda_speak, ""No target. Press tab to choose."");
    }

    // Y: diagnostic - raw object names within ~80px.
    if (keyboard_check_pressed(ord(""Y"")))
    {
        var _msg = """";
        var _cnt = 0;
        with (all)
        {
            if (id != other.id)
            {
                var _dist = point_distance(other.x, other.y, x, y);
                if (_dist < 80)
                {
                    var _dx = x - other.x; var _dy = y - other.y;
                    var _dr = """";
                    if (_dy < -12) _dr = ""up""; else if (_dy > 12) _dr = ""down"";
                    if (_dx < -12) { if (_dr != """") _dr += "" ""; _dr += ""left""; } else if (_dx > 12) { if (_dr != """") _dr += "" ""; _dr += ""right""; }
                    if (_dr == """") _dr = ""on"";
                    _msg += object_get_name(object_index) + "" "" + _dr + "". "";
                    _cnt += 1;
                }
            }
        }
        if (_cnt == 0) _msg = ""Nothing within range."";
        external_call(global.nvda_speak, _msg);
    }

    if (movement == 1)
    {
        var _walking = (variable_global_exists(""nvda_walk_active"") && global.nvda_walk_active == 1);
        if (nvda_startdelay > 0)
        {
            nvda_startdelay -= 1;
        }
        else if (!_walking && variable_global_exists(""nvda_sel"") && global.nvda_sel != noone && instance_exists(global.nvda_sel))
        {
            var _t = global.nvda_sel;
            var _dx = _t.x - x; var _dy = _t.y - y;
            var _steps = round(point_distance(x, y, _t.x, _t.y) / 20);
            var _dir = """";
            if (_dy < -16) _dir = ""up""; else if (_dy > 16) _dir = ""down"";
            if (_dx < -16) { if (_dir != """") _dir += "" and ""; _dir += ""left""; } else if (_dx > 16) { if (_dir != """") _dir += "" and ""; _dir += ""right""; }
            if (_dir == """") _dir = ""here"";
            if (_dir != nvda_lastdir)
            {
                nvda_lastdir = _dir;
                if (_dir == ""here"") external_call(global.nvda_speak, ""Reached"");
                else external_call(global.nvda_speak, _dir + "", "" + string(_steps) + "" steps"");
            }
        }

        var _tried = (obj_time.up || obj_time.down || obj_time.left || obj_time.right);
        if (_tried && x == xprevious && y == yprevious && !_walking)
        {
            if (nvda_blocktimer <= 0)
            {
                external_call(global.nvda_speak, ""Blocked"");
                nvda_blocktimer = 30;
            }
        }
        if (nvda_blocktimer > 0) nvda_blocktimer -= 1;

        // Hole proximity warning (avoid: holes drop you to a lower floor).
        var _hole = instance_nearest(x, y, obj_holedown);
        var _sh = instance_nearest(x, y, obj_superdrophole);
        if (_sh != noone && instance_exists(_sh))
        {
            if (_hole == noone || !instance_exists(_hole) || point_distance(x, y, _sh.x, _sh.y) < point_distance(x, y, _hole.x, _hole.y))
                _hole = _sh;
        }
        if (_hole != noone && instance_exists(_hole))
        {
            if (point_distance(x, y, _hole.x, _hole.y) < 34)
            {
                if (nvda_holetimer <= 0)
                {
                    var _hdx = _hole.x - x; var _hdy = _hole.y - y;
                    var _hdir = """";
                    if (_hdy < -12) _hdir = ""up""; else if (_hdy > 12) _hdir = ""down"";
                    if (_hdx < -12) { if (_hdir != """") _hdir += "" ""; _hdir += ""left""; } else if (_hdx > 12) { if (_hdir != """") _hdir += "" ""; _hdir += ""right""; }
                    if (_hdir == """") _hdir = ""here"";
                    external_call(global.nvda_speak, ""Hole "" + _hdir);
                    nvda_holetimer = 20;
                }
            }
        }
        if (nvda_holetimer > 0) nvda_holetimer -= 1;
    }
    }
}";

var importGroup = new UndertaleModLib.Compiler.CodeImportGroup(Data);
importGroup.QueueAppend(Data.Code.ByName("gml_Object_obj_mainchara_Create_0"), createGml);
importGroup.QueueAppend(Data.Code.ByName("gml_Object_obj_mainchara_Step_0"), stepGml);
importGroup.Import();
Console.WriteLine("Injected NVDA navigation v4: unified interactable cursor (Tab/Shift+Tab/E/G) + live guidance + blocked");

}

// ===== inject_wasd.csx =====
{
// Undertale accessibility - WASD overworld movement.
// Arrow keys interrupt the screen reader, so add W/A/S/D as a second walking input.
// obj_time Begin Step (Step_1) computes up/down/left/right (read by obj_mainchara Step).
// On Windows (osflavor==1) it uses keyboard_check_DIRECT for arrows, which keyboard_key_press
// can't simulate -> so we set up/down/left/right DIRECTLY here, after the game's own logic.
// Arrows still work; this just adds WASD on top. (Menus still use arrows for now.)

string[] need = { "keyboard_check", "ord" };
foreach (var f in need) Data.Functions.EnsureDefined(f, Data.Strings);

string gml = @"
{
    if (keyboard_check(ord(""W""))) up = 1;
    if (keyboard_check(ord(""S""))) down = 1;
    if (keyboard_check(ord(""A""))) left = 1;
    if (keyboard_check(ord(""D""))) right = 1;
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_time_Step_1"), gml);
g.Import();
Console.WriteLine("Injected WASD overworld movement (obj_time Begin Step)");

}

// ===== inject_battle.csx =====
{
// Undertale accessibility - battle menu announcer.
// Appends a per-frame state-watcher to obj_battlecontroller Draw_0 (state is final there).
// Speaks on change of menu state. Battle flavor/result text is already narrated via OBJ_WRITER
// (inherits obj_base_writer dialogue hook). The dodge phase is M4 (separate).
//
// global.bmenuno: 0=buttons, 1/11=FIGHT target, 2=ACT target, 10=ACT options, 3/3.5=ITEM, 4=MERCY.
// global.bmenucoord[0]=button(0 FIGHT,1 ACT,2 ITEM,3 MERCY); [1]=target; [2]=act option; [3]=item; [4]=mercy(0 Spare,1 Flee).
// ACT option text packed in global.msg[0] e.g. "   * Check         * Talk" (cols=spaces, rows=&; slots 0-2 left,3-5 right).

string[] need = { "external_define","external_call","variable_global_exists","variable_instance_exists",
                  "string","string_pos","string_copy","string_length","string_char_at","round","instance_exists",
                  "keyboard_check_pressed","ord","string_lower" };
foreach (var f in need) Data.Functions.EnsureDefined(f, Data.Strings);

string bridge = @"
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }";

// SPAREABLE check: replicates the game's own yellow-name logic (SCR_TEXT case 3) -
// a monster's name is drawn yellow when monsterinstance[i].mercy < 0 after scr_mercystandard,
// where mercy = (monsterhp - global.at - global.wstrength + monsterdef) - mercymod.
// Sets a GML local _sp (0/1) for the monster slot in <idxExpr>.
string Spareable(string idxExpr) => @"
            var _sp = 0;
            var _spi = " + idxExpr + @";
            if (_spi >= 0 && _spi < 3 && global.monster[_spi] == 1)
            {
                var _mi = global.monsterinstance[_spi];
                if (instance_exists(_mi) && variable_instance_exists(_mi, ""mercymod""))
                {
                    var _mm = ((global.monsterhp[_spi] - global.at - global.wstrength) + global.monsterdef[_spi]) - _mi.mercymod;
                    if (_mm < 0) _sp = 1;
                }
            }";

string gml = @"
{
    if (variable_global_exists(""nvda_opt_speech"") && global.nvda_opt_speech == 0) exit;" + bridge + @"
    if (!variable_instance_exists(id, ""nvda_bset""))
    {
        nvda_bset = 1; nvda_pmenu = -99; nvda_pc0 = -99; nvda_pc1 = -99;
        nvda_pc2 = -99; nvda_pc3 = -99; nvda_pc4 = -99; nvda_pinmenu = 0; nvda_prefix = """";
        nvda_target_idx = -99;
    }

    var _inmenu = (active == 1 && global.mnfight == 0 && global.myfight == 0 && global.bmenuno == 0);

    // Turn start: announce HP + currently highlighted button (one speak, no collision).
    if (_inmenu && !nvda_pinmenu)
    {
        var _b = ""Fight"";
        if (global.bmenucoord[0] == 1) _b = ""Act""; else if (global.bmenucoord[0] == 2) _b = ""Item""; else if (global.bmenucoord[0] == 3) _b = ""Mercy"";
        external_call(global.nvda_speak, ""Your turn. H P "" + string(global.hp) + "" of "" + string(global.maxhp) + "". "" + _b);
        nvda_pc0 = global.bmenucoord[0];
    }
    nvda_pinmenu = _inmenu;

    // Submenu change: reset cursor trackers + set a one-shot prefix for the next announce.
    if (global.bmenuno != nvda_pmenu)
    {
        nvda_pmenu = global.bmenuno;
        nvda_pc1 = -99; nvda_pc2 = -99; nvda_pc3 = -99; nvda_pc4 = -99;
        nvda_prefix = """";
        if (global.bmenuno == 1 || global.bmenuno == 11) nvda_prefix = ""Fight. "";
        else if (global.bmenuno == 2) nvda_prefix = ""Act on. "";
        else if (global.bmenuno == 10) nvda_prefix = ""Act. "";
        else if (global.bmenuno >= 3 && global.bmenuno < 4) nvda_prefix = ""Item. "";
        else if (global.bmenuno == 4) nvda_prefix = ""Mercy. "";
    }

    // Buttons (top row).
    if (global.bmenuno == 0 && _inmenu)
    {
        if (global.bmenucoord[0] != nvda_pc0)
        {
            nvda_pc0 = global.bmenucoord[0];
            var _b = ""Fight"";
            if (nvda_pc0 == 1) _b = ""Act""; else if (nvda_pc0 == 2) _b = ""Item""; else if (nvda_pc0 == 3) _b = ""Mercy"";
            external_call(global.nvda_speak, _b);
        }
    }

    // Target select (FIGHT / ACT): monster name + spareable + HP.
    if (global.bmenuno == 1 || global.bmenuno == 2 || global.bmenuno == 11)
    {
        if (global.bmenucoord[1] != nvda_pc1)
        {
            nvda_pc1 = global.bmenucoord[1];
            nvda_target_idx = nvda_pc1;
            var _nm = global.monstername[nvda_pc1];
            var _hp = """";
            if (global.monstermaxhp[nvda_pc1] > 0) _hp = "", H P "" + string(global.monsterhp[nvda_pc1]) + "" of "" + string(global.monstermaxhp[nvda_pc1]);" + Spareable("nvda_pc1") + @"
            var _sps = """";
            if (_sp) _sps = "", spareable"";
            external_call(global.nvda_speak, nvda_prefix + _nm + _sps + _hp);
            nvda_prefix = """";
        }
    }

    // ACT options: parse label for current slot from global.msg[0].
    if (global.bmenuno == 10)
    {
        if (global.bmenucoord[2] != nvda_pc2)
        {
            nvda_pc2 = global.bmenucoord[2];
            var _slot = nvda_pc2;
            var _row; var _col;
            if (_slot < 3) { _row = _slot; _col = 0; } else { _row = _slot - 3; _col = 1; }
            var _s = global.msg[0];
            var _ri = 0; var _ok = 1;
            while (_ri < _row)
            {
                var _p = string_pos(""&"", _s);
                if (_p == 0) { _ok = 0; break; }
                _s = string_copy(_s, _p + 1, string_length(_s) - _p);
                _ri += 1;
            }
            var _lbl = """";
            if (_ok)
            {
                var _pe = string_pos(""&"", _s);
                if (_pe > 0) _s = string_copy(_s, 1, _pe - 1);
                var _ci = 0; var _cur = _s;
                repeat (6)
                {
                    var _ap = string_pos(""* "", _cur);
                    if (_ap == 0) break;
                    var _after = string_copy(_cur, _ap + 2, string_length(_cur) - (_ap + 1));
                    var _np = string_pos(""* "", _after);
                    var _opt;
                    if (_np > 0) _opt = string_copy(_after, 1, _np - 1); else _opt = _after;
                    if (_ci == _col) { _lbl = _opt; break; }
                    _ci += 1;
                    _cur = _after;
                }
            }
            while (string_length(_lbl) > 0 && string_char_at(_lbl, 1) == "" "") _lbl = string_copy(_lbl, 2, string_length(_lbl) - 1);
            while (string_length(_lbl) > 0 && string_char_at(_lbl, string_length(_lbl)) == "" "") _lbl = string_copy(_lbl, 1, string_length(_lbl) - 1);
            if (_lbl == """") _lbl = ""Option "" + string(nvda_pc2 + 1);
            external_call(global.nvda_speak, nvda_prefix + _lbl);
            nvda_prefix = """";
        }
    }

    // ITEM: name from global.itemnameb[itempos].
    if (global.bmenuno >= 3 && global.bmenuno < 4)
    {
        if (global.bmenucoord[3] != nvda_pc3)
        {
            nvda_pc3 = global.bmenucoord[3];
            var _ip = global.bmenucoord[3] + round((global.bmenuno - 3) * 8);
            var _nm = global.itemnameb[_ip];
            while (string_length(_nm) > 0 && string_char_at(_nm, string_length(_nm)) == "" "") _nm = string_copy(_nm, 1, string_length(_nm) - 1);
            external_call(global.nvda_speak, nvda_prefix + _nm);
            nvda_prefix = """";
        }
    }

    // MERCY: Spare / Flee. On Spare, flag whether any enemy can be spared right now.
    if (global.bmenuno == 4)
    {
        if (global.bmenucoord[4] != nvda_pc4)
        {
            nvda_pc4 = global.bmenucoord[4];
            var _m = ""Spare"";
            if (nvda_pc4 == 1) _m = ""Flee"";
            else
            {
                var _anysp = 0;
                for (var _k = 0; _k < 3; _k += 1)
                {" + Spareable("_k") + @"
                    if (_sp) _anysp = 1;
                }
                if (_anysp) _m = ""Spare, can spare now"";
            }
            external_call(global.nvda_speak, nvda_prefix + _m);
            nvda_prefix = """";
        }
    }

    // ---- Turn-free DESCRIBE key (D): read a physical description of the monster you're
    // targeting. Works during FIGHT/ACT target-select (bmenuno 1/2/11) and the ACT options
    // menu (10). It only SPEAKS - it never selects an act - so it never consumes a turn.
    // The whole point of pacifist Undertale is not hurting the cute little monsters; this is
    // how a blind player finds out they're cute.
    if (keyboard_check_pressed(ord(""D"")))
    {
        var _ti = -1;
        if (global.bmenuno == 1 || global.bmenuno == 2 || global.bmenuno == 11) _ti = global.bmenucoord[1];
        else if (global.bmenuno == 10) _ti = nvda_target_idx;
        if (_ti >= 0)
        {
            var _mn = global.monstername[_ti];
            var _lc = string_lower(_mn);
            while (string_length(_lc) > 0 && string_char_at(_lc, 1) == "" "") _lc = string_copy(_lc, 2, string_length(_lc) - 1);
            while (string_length(_lc) > 0 && string_char_at(_lc, string_length(_lc)) == "" "") _lc = string_copy(_lc, 1, string_length(_lc) - 1);
            var _md = """";
            if (_lc == ""froggit"" || _lc == ""final froggit"") _md = ""A plump little frog-like monster, pale and soft, that sits blinking up at you with big round eyes and a wide, gentle mouth."";
            else if (_lc == ""whimsun"") _md = ""A tiny, timid winged monster like a small moth. It has a little downturned face and trembles nervously, as if it is sorry to be fighting at all."";
            else if (_lc == ""moldsmal"") _md = ""A wobbly mound of pale green jelly-mold. It jiggles gently in place and does not seem to have any idea how to hurt anyone."";
            else if (_lc == ""migosp"") _md = ""A small dark beetle-like bug monster with round eyes and little waving arms. Away from a crowd it is shy and harmless."";
            else if (_lc == ""vegetoid"") _md = ""A big, friendly vegetable monster rising out of the ground, shaped like a giant carrot with a wide grinning face full of blocky teeth."";
            else if (_lc == ""loox"") _md = ""A round little monster covered in soft fur, with one big eye in the middle of its face. It looks grumpy, but really it just does not want to be picked on."";
            else if (_lc == ""napstablook"") _md = ""A small, shy white ghost with a droopy face and half-closed sleepy eyes. It drifts quietly and always seems on the edge of tears."";
            else if (_lc == ""dummy"") _md = ""A cloth training dummy on a wooden stand, with a plain stitched-on face. It stands there silently, waiting."";
            else if (_lc == ""toriel"") _md = ""The tall, gentle goat-like monster in a long purple robe, standing between you and the door with a pained, protective look."";
            else if (_lc == ""snowdrake"") _md = ""A small blue bird-monster built for the cold, with a crest of icy feathers. A teenager doing its best to land a good joke."";
            else if (_lc == ""ice cap"" || _lc == ""icecap"") _md = ""A little furry monster nearly hidden beneath an enormous pointed cap of ice that it is extremely proud of. Only its eyes peek out from underneath."";
            else if (_lc == ""gyftrot"") _md = ""A shaggy, weary deer-like monster whose antlers have been draped with junk by prank-playing kids. It just wants them taken off."";
            else if (_lc == ""doggo"") _md = ""A dog-monster sitting in a wooden sentry post, gripping two glowing daggers, eyes darting about. It can only see things that move."";
            else if (_lc == ""lesser dog"") _md = ""A small white dog-monster in a suit of armour, holding a sword, with a cheerfully lolling tongue and a neck that stretches longer the more excited it gets."";
            else if (_lc == ""greater dog"") _md = ""A little white dog almost lost inside a huge suit of armour, tail wagging eagerly. It would much rather play than fight."";
            else if (_lc == ""dogamy"") _md = ""One of a married pair of dog-monsters in black hooded robes, swinging a large axe and sniffing the air for your scent."";
            else if (_lc == ""dogaressa"") _md = ""One of a married pair of dog-monsters in black hooded robes, padding beside her husband with an axe in paw."";
            else if (_lc == ""papyrus"") _md = ""The very tall, lanky skeleton in his homemade white costume and long red scarf, striking a heroic pose as he faces you."";
            else if (_lc == ""chilldrake"") _md = ""A cheeky blue bird-monster, cousin of the Snowdrake, with an icy feathered crest and an even cooler attitude."";
            // ---- Waterfall ----
            else if (_lc == ""mad dummy"") _md = ""A cloth dummy floating in the air, shaking with rage as an angry ghost throws its voice through it. Its stitched face is twisted into a furious scowl."";
            else if (_lc == ""aaron"") _md = ""A muscular seahorse-like monster with a confident grin, endlessly flexing his enormous arms. He is far more interested in showing off than fighting."";
            else if (_lc == ""woshua"") _md = ""A small blue monster shaped a bit like a crab, with a scrubbing brush for a crest. It is obsessed with cleanliness and just wants everything tidy."";
            else if (_lc == ""moldbygg"") _md = ""A tall, wobbling column of pale green mold, like a taller cousin of the Moldsmal. It sways gently and loves a good hug."";
            else if (_lc == ""temmie"") _md = ""A strange little creature drawn in a crude, scribbly style: a cat-like face with big ears and wide eyes on a small brown furry body."";
            else if (_lc == ""shyren"") _md = ""A shy mermaid-like monster who hides her face, humming a quiet, wavering tune. She is far too bashful to look at you directly."";
            else if (_lc == ""glyde"") _md = ""A fuzzy, cloud-like monster in a bomber jacket, drifting with an awkward, trying-too-hard cool. A rare wanderer not quite sure it belongs."";
            else if (_lc == ""jerry"") _md = ""A small, lumpy grey monster with a permanently smug, oblivious expression. Frankly, nobody enjoys having Jerry around."";
            else if (_lc == ""undyne"") _md = ""A tall, powerful fish-warrior in gleaming armour, red hair streaming and one eye blazing behind an eyepatch as she summons glowing spears. The head of the Royal Guard, and she will not back down."";
            // ---- Hotland and the CORE ----
            else if (_lc == ""vulkin"") _md = ""A small, round volcano-monster glowing warm at its crater, with stubby arms and an eager, beaming face. It only wants to help, even when its help burns a little."";
            else if (_lc == ""tsunderplane"") _md = ""A fighter-plane monster with a blushing, bashful face on its nose. It insists it is absolutely not flying this close to you on purpose."";
            else if (_lc == ""pyrope"") _md = ""A round, flaming monster like a living ember with a wide grin, radiating heat and wanting everything hotter."";
            else if (_lc == ""madjick"") _md = ""A tall, cloaked wizard-monster in a wide, pointed hat, with two glowing magic orbs circling it. Only its glinting eyes show beneath the brim."";
            else if (_lc == ""knight knight"") _md = ""An enormous, heavily armoured monster like a mountain of a knight, with a crescent-moon helm and a massive mace. Slow, sleepy, and immensely strong."";
            else if (_lc == ""final froggit"") _md = ""A tougher, older Froggit from the deeper Underground. The same soft, pale frog body and big eyes, but with a wiser, more determined look."";
            else if (_lc == ""whimsalot"") _md = ""A Whimsun grown into a tiny armoured knight, with little wings, a helmet and a spear, bravely trying to look fierce."";
            else if (_lc == ""astigmatism"") _md = ""A round yellow monster with one big eye and a wide, toothy mouth, glaring sharply. It is very insistent that you pay attention."";
            else if (_lc == ""migospel"") _md = ""A Migosp that has found its confidence: a little beetle-monster waving its arms cheerfully to a beat only it can hear."";
            else if (_lc == ""so sorry"") _md = ""A small, flustered dragon-like monster in formal wear, tripping over its own apologies. It genuinely did not mean to be here and is terribly sorry."";
            else if (_lc == ""muffet"") _md = ""An elegant purple spider-monster with five eyes and five arms, in a frilly outfit, a teacup balanced daintily in one hand as her pet spiders skitter around her."";
            else if (_lc == ""mettaton"") _md = ""The rectangular metal box robot on a single wheel, dials and a screen on its front and slim arms out to either side, hosting this like a dazzling TV show."";
            else if (_lc == ""mettaton ex"" || _lc == ""mettatonex"") _md = ""A fabulous robot in a sleek humanoid form, all black and pink, balanced on one wheeled leg in a dramatic pose under the stage lights."";
            else if (_lc == ""mettaton neo"") _md = ""A towering battle-form robot bristling with cannons and armour, wings spread, built to look utterly unstoppable."";
            // ---- Bosses and the amalgamates ----
            else if (_lc == ""asgore"") _md = ""An enormous goat-king in purple armour over a royal cape, huge and broad-shouldered, with long curved horns and a golden beard. He lifts a great trident, and his eyes are full of sorrow."";
            else if (_lc == ""sans"") _md = ""The short skeleton in the blue hooded jacket, hands in his pockets, grinning as ever, one eye flickering with a strange blue light. He does not seem to be taking this seriously, which is the most dangerous thing about him."";
            else if (_lc == ""endogeny"") _md = ""A large, unsettling amalgam of many dog-monsters melted together, a dripping white mass with a single dog-like face and too many limbs. It bounds toward you wanting to play."";
            else if (_lc == ""reaper bird"") _md = ""A tall, eerie amalgam of bird-monsters, dark-winged with a long neck and a hollow, staring face. It drifts unnaturally, several beings at once."";
            else if (_lc == ""lemon bread"") _md = ""A pale amalgam of monster-parts fused into a long, finned, snake-like body with too many mouths, weaving with an odd grace."";
            else if (_lc == ""memoryhead"") _md = ""A drifting, half-formed amalgam like a melting white face on a stalk, murmuring softly and reaching toward you as if it knows you."";
            else if (_lc == ""snowdrake's mother"" || _lc == ""snowman"" || string_pos(""snowdrake's"", _lc) > 0) _md = ""A large, gentle bird-monster, mother of the little Snowdrake, her feathers pale and soft. She carries a quiet, tired sadness."";
            // ---- Hard-mode Ruins variants ----
            else if (_lc == ""moldessa"") _md = ""A hard-mode cousin of the Moldsmal: a wobbling mound of mold with a bit more attitude."";
            else if (_lc == ""parsnik"") _md = ""A hard-mode cousin of the Vegetoid: a large root-vegetable monster with a snappier grin."";
            if (_md == """") _md = ""No description written yet for "" + _mn + ""."";
            external_call(global.nvda_speak, _md);
        }
    }
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_battlecontroller_Draw_0"), gml);
g.Import();
Console.WriteLine("Injected battle menu announcer (buttons/target/act/item/mercy + HP + describe key)");

}

// ===== inject_fight.csx =====
{
// Undertale accessibility - FIGHT attack-bar audio cue.
// obj_targetchoice = the slider moving across obj_target. Press Z when slider center is within
// 12px of bar center for max damage; runs off the end = 0 damage (miss).
// We: (1) slow the slider (assist), (2) "parking-sensor" beeps - faster + higher pitch nearer
// center, peak = press point, (3) announce damage on hit.
// snd_play uses audio_play_sound, so native audio_play_sound/audio_sound_pitch work here.

string[] need = { "external_define","external_call","variable_global_exists","variable_instance_exists",
                  "string","abs","round","instance_exists","audio_play_sound","audio_sound_pitch" };
foreach (var f in need) Data.Functions.EnsureDefined(f, Data.Strings);

string bridge = @"
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }";

string createGml = @"
{
    if (variable_global_exists(""nvda_opt_combat"") && global.nvda_opt_combat == 0) exit;" + bridge + @"
    hspeed *= 0.5;
    nvda_beeptimer = 0;
    nvda_announced = 0;
    external_call(global.nvda_speak, ""Attack. Press Z at the highest beep."");
}";

string stepGml = @"
{
    if (variable_global_exists(""nvda_opt_combat"") && global.nvda_opt_combat == 0) exit;
    if (!variable_instance_exists(id, ""nvda_announced"")) nvda_announced = 0;
    if (!variable_instance_exists(id, ""nvda_beeptimer"")) nvda_beeptimer = 0;
    if (image_speed == 0)
    {
        if (instance_exists(obj_target))
        {
            var _myx = x + (sprite_width / 2);
            var _cx = obj_target.x + (obj_target.sprite_width / 2);
            var _d = abs(_myx - _cx);
            var _maxd = obj_target.sprite_width / 2;
            if (_maxd <= 0) _maxd = 1;
            var _close = 1 - (_d / _maxd);
            if (_close < 0) _close = 0;
            if (nvda_beeptimer <= 0)
            {
                var _snd = audio_play_sound(snd_squeak, 50, false);
                audio_sound_pitch(_snd, 0.7 + (_close * 1.7));
                nvda_beeptimer = round(13 - (_close * 11));
                if (nvda_beeptimer < 2) nvda_beeptimer = 2;
            }
            nvda_beeptimer -= 1;
        }
    }
    else
    {
        if (nvda_announced == 0)
        {
            nvda_announced = 1;
            external_call(global.nvda_speak, ""Hit. "" + string(global.damage) + "" damage."");
        }
    }
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_targetchoice_Create_0"), createGml);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_targetchoice_Step_0"), stepGml);
g.Import();
Console.WriteLine("Injected FIGHT attack-bar audio cue (slow + parking-sensor beeps + damage announce)");

}

// ===== inject_goldbox.csx =====
{
// Undertale accessibility - speak the spider-sale / purchase gold+space HUD.
// obj_golddisplay draws "$ - <gold>G" and "SPACE - <held>/8" directly via draw_text
// every frame, so a screen reader never gets it.  We append a ONE-SHOT announcement
// to its Draw event (guarded so it speaks once per appearance, not 30x/sec), worded
// for speech, and QUEUED so it follows the spider's dialogue instead of cutting it off.
// The instance self-destructs when the dialogue writer closes, so a fresh purchase
// makes a new instance -> re-announces updated gold after each buy.

Data.Functions.EnsureDefined("external_define", Data.Strings);
Data.Functions.EnsureDefined("external_call", Data.Strings);
Data.Functions.EnsureDefined("variable_global_exists", Data.Strings);
Data.Functions.EnsureDefined("variable_instance_exists", Data.Strings);
Data.Functions.EnsureDefined("string", Data.Strings);

string gml = @"
if (variable_global_exists(""nvda_opt_speech"") && global.nvda_opt_speech == 0) exit;
if (!variable_instance_exists(id, ""nvda_said""))
{
    nvda_said = 1;
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }
    if (!variable_global_exists(""nvda_queue""))
        global.nvda_queue = external_define(""nvda_gm.dll"", ""gmnvda_speak_queue"", 0, 0, 1, 1);
    // itemhold / itemfree were set by scr_itemroom() earlier this Draw.
    var _msg = ""You have "" + string(global.gold) + "" gold. "" + string(itemfree) + "" of 8 inventory slots free."";
    external_call(global.nvda_queue, _msg);
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_golddisplay_Draw_0"), gml);
g.Import();
Console.WriteLine("Injected gold/space HUD announcer (obj_golddisplay Draw, one-shot queued)");

}

// ===== inject_dodge.csx =====
{
// Undertale accessibility - M4 dodge assist v2: SAFE-SPOT TELEPORT (Lilian's design).
//   (1)  Assisted/Normal toggle (safety net) - key M, hosted in persistent obj_time.
//   (1b/c) HP safety net so assisted mode truly can't die (clamp + death-object guard).
//   (2)  SAFE-SPOT CUE + TELEPORT - appended to obj_heart Step_0, red soul (movement 1):
//          * scan a grid over the battle box, score each cell by clearance from the
//            nearest bullet, pick the clearest cell = the SAFE SPOT.
//          * play a panned beep pointing at it: PAN = horizontal (left ear / right ear),
//            PITCH = vertical (high = up, low = down).  Panning comes from gmpan.dll
//            (this PC port's own panning is a disabled stub).
//          * press the matching arrow (or WASD) -> TELEPORT the heart to the safe spot,
//            with a short centred 'landed' beep.
//   (3)  Locator - key Q: speaks the SOUL's 3x3 position in the box.
// Blue-soul jump attacks (movement 2, e.g. Papyrus) and Sans' separate heart are left
// for a later pass; v2 targets standard red-soul dodging (Toriel and most monsters).

foreach (var fn in new[]{
    "external_define","external_call","variable_global_exists","variable_instance_exists",
    "keyboard_check_pressed","keyboard_check","ord","max","round","string","instance_destroy","instance_exists"
}) Data.Functions.EnsureDefined(fn, Data.Strings);

// NVDA speech bridge (used by M toggle + Q locator)
string bridge = @"
if (!variable_global_exists(""nvda_ready""))
{
    global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
    global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
    external_call(global.nvda_init, """");
    global.nvda_ready = 1;
}";

// Panned-beep bridge (used by the safe-spot cue) - gmpan.dll, ships beside the game exe.
//   gmpan_init()                      -> 0 args
//   gmpan_beep(pan, freq, ms, gain)   -> 4 real args  (pan -1 left .. +1 right)
string panbridge = @"
if (!variable_global_exists(""pan_ready""))
{
    global.pan_init = external_define(""gmpan.dll"", ""gmpan_init"", 0, 0, 0);
    global.pan_beep = external_define(""gmpan.dll"", ""gmpan_beep"", 0, 0, 4, 0, 0, 0, 0);
    external_call(global.pan_init);
    global.pan_ready = 1;
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);

// --- (1) Difficulty mode cycle + per-frame safety net, hosted in persistent obj_time ---
// M cycles three tiers:  0 = Assisted (can't die)  ->  1 = Slow (can die, game clock
// halved DURING FIGHTS ONLY for reaction time)  ->  2 = Normal (full speed, can die).
// global.nvda_assist stays in sync (==1 only in mode 0) so the existing safety-net
// clamps in scr_damagestandard / obj_heartdefeated keep working unchanged.
string toggle = @"
if (!variable_global_exists(""nvda_mode""))
{
    global.nvda_mode = 0;
    global.nvda_assist = 1;
    global.nvda_slowed = 0;
}
global.nvda_assist = (global.nvda_mode == 0);
if (global.nvda_assist == 1 && global.hp < 1)
    global.hp = 1;
// Photoshop Flowey (neutral final boss) uses its OWN hp var (global.my_hp) and its own
// death object (obj_vsflowey_heartdefeated, spawned from obj_vsflowey_heart when
// my_hp<=0). This BeginStep clamp runs before that Step-phase death check, so assisted
// mode can't die here either.
if (global.nvda_assist == 1 && variable_global_exists(""my_hp"") && global.my_hp < 1)
    global.my_hp = 1;
// room_speed control (only while a fight is active): HOLD F = fast-forward (blast
// through long survival waves -- great for the Photoshop Flowey slog while invincible);
// otherwise Slow mode halves it; otherwise restore to normal. Covers the normal battle
// controller AND the Flowey fight (which has no battlecontroller).
var _infight = (instance_exists(obj_battlecontroller) || instance_exists(obj_flowey_master));
if (_infight && keyboard_check(ord(""F"")))
{
    global.nvda_slowed = 1;
    room_speed = 120;
}
else if (global.nvda_mode == 1 && _infight)
{
    global.nvda_slowed = 1;
    room_speed = 15;
}
else if (variable_global_exists(""nvda_slowed"") && global.nvda_slowed == 1)
{
    global.nvda_slowed = 0;
    room_speed = 30;
}
if (keyboard_check_pressed(ord(""M"")))
{" + bridge + @"
    global.nvda_mode += 1;
    if (global.nvda_mode > 2) global.nvda_mode = 0;
    if (global.nvda_mode == 0)
        external_call(global.nvda_speak, ""Assisted mode. You cannot be defeated."");
    else if (global.nvda_mode == 1)
        external_call(global.nvda_speak, ""Slow mode. Fights run at half speed. You can be defeated."");
    else
        external_call(global.nvda_speak, ""Normal mode. Full speed. You can be defeated."");
}";
g.QueueAppend(Data.Code.ByName("gml_Object_obj_time_Step_1"), toggle);

// --- (1b) Safety-net clamp in the named damage chokepoints ---
string clamp = @"
if (variable_global_exists(""nvda_assist"") && global.nvda_assist == 1 && global.hp < 1)
    global.hp = 1;";
g.QueueAppend(Data.Code.ByName("gml_Script_scr_damagestandard"), clamp);
g.QueueAppend(Data.Code.ByName("gml_Script_scr_damagestandard_x"), clamp);

// --- (1c) Guard the death object for bullets that create it inline ---
string nodeath = @"
if (variable_global_exists(""nvda_assist"") && global.nvda_assist == 1)
{
    global.hp = global.maxhp;
    instance_destroy();
}";
g.QueueAppend(Data.Code.ByName("gml_Object_obj_heartdefeated_Create_0"), nodeath);

// --- (2) Safe-spot cue + teleport, appended to obj_heart Step_0 (red soul) ---
// All bullet families to scan for the safe-spot finder. BOSS bullets use their own
// parent objects (Muffet = obj_spiderbulletparent, Asgore = obj_asgorebulparent /
// obj_asbulletparent, etc.) that do NOT descend from blt_parent - so the old 3-family
// scan found zero bullets in boss fights and never beeped. Filter to objects that
// actually exist in this data, then generate one with(...) collection block per family.
string[] bulletObjs = {
    "blt_parent", "blt_parent_noborder", "obj_sansbullet_parent",
    "obj_spiderbulletparent", "obj_asgorebulparent", "obj_asbulletparent",
    "obj_amalgambul_parent", "obj_floweybullet_parent", "obj_metttestbulletparent",
    "obj_astigmatism_bullet", "obj_6gun_bullet", "obj_donutbullet",
    "obj_croissant", "obj_vertcroissant", "obj_spiderbullet",
    // Undyne in-battle red-soul spears (follow = home-then-dash; rise = 3 columns):
    "obj_spearbullet_follow", "obj_risespearbullet", "obj_followspear_2",
    "obj_rotspear", "obj_undynespear"
};
var _bsb = new System.Text.StringBuilder();
foreach (var bo in bulletObjs)
{
    bool _ex = false;
    foreach (var o in Data.GameObjects) { if (o.Name.Content == bo) { _ex = true; break; } }
    if (_ex)
        _bsb.Append("    with (" + bo + ") { if (global.nvda_nb < 120) { global.nvda_bx[global.nvda_nb] = x; global.nvda_byy[global.nvda_nb] = y; global.nvda_nb += 1; } }\n");
}
string bulletCollect = _bsb.ToString();
Console.WriteLine("Dodge scan covers " + bulletCollect.Split('\n').Length + " bullet families");

string safespot = @"
if (variable_global_exists(""nvda_opt_combat"") && global.nvda_opt_combat == 0) exit;
if (variable_instance_exists(id, ""movement"") && movement == 1 && global.mnfight == 2)
{" + panbridge + @"
    // Gather bullet positions once (cap for cost), then reuse for the grid scan.
    global.nvda_nb = 0;
" + bulletCollect + @"
    if (!variable_instance_exists(id, ""nvda_scantimer"")) nvda_scantimer = 0;
    if (!variable_instance_exists(id, ""nvda_cuetimer""))  nvda_cuetimer  = 0;
    if (!variable_instance_exists(id, ""nvda_safex""))     { nvda_safex = x; nvda_safey = y; }

    // Reachable interior of the box (matches the heart's own clamp margins).
    var _bx0 = global.idealborder[0] + 8;
    var _bx1 = global.idealborder[1] - 16;
    var _by0 = global.idealborder[2] + 8;
    var _by1 = global.idealborder[3] - 16;
    var _hw = (_bx1 - _bx0) / 2; if (_hw < 1) _hw = 1;
    var _hh = (_by1 - _by0) / 2; if (_hh < 1) _hh = 1;

    // Rescan the safe spot every 2 frames (the field moves fast; this bounds cost).
    nvda_scantimer -= 1;
    if (global.nvda_nb > 0 && nvda_scantimer <= 0)
    {
        nvda_scantimer = 2;
        var _cols = 7; var _rows = 5;
        var _best = -1; var _bestx = x; var _besty = y;
        var _gx; var _gy; var _k;
        for (_gx = 0; _gx < _cols; _gx += 1)
        {
            for (_gy = 0; _gy < _rows; _gy += 1)
            {
                var _cx = _bx0 + (_bx1 - _bx0) * _gx / (_cols - 1);
                var _cy = _by0 + (_by1 - _by0) * _gy / (_rows - 1);
                var _mind = 99999999;
                for (_k = 0; _k < global.nvda_nb; _k += 1)
                {
                    var _ex = _cx - global.nvda_bx[_k];
                    var _ey = _cy - global.nvda_byy[_k];
                    var _sq = (_ex * _ex) + (_ey * _ey);   // squared dist (no sqrt)
                    if (_sq < _mind) _mind = _sq;
                }
                if (_mind > _best) { _best = _mind; _bestx = _cx; _besty = _cy; }
            }
        }
        nvda_safex = _bestx;
        nvda_safey = _besty;
    }

    // Cue beep toward the safe spot: pan = sideways, pitch = up/down.
    nvda_cuetimer -= 1;
    if (global.nvda_nb > 0 && nvda_cuetimer <= 0)
    {
        nvda_cuetimer = 18;
        var _pan = (nvda_safex - x) / _hw;
        if (_pan < -1) _pan = -1; if (_pan > 1) _pan = 1;
        var _nv = (y - nvda_safey) / _hh;          // + = safe spot is above = up
        if (_nv < -1) _nv = -1; if (_nv > 1) _nv = 1;
        var _freq = 440 + (_nv * 280);
        if (_freq < 200) _freq = 200; if (_freq > 800) _freq = 800;
        external_call(global.pan_beep, _pan, _freq, 120, 0.7);
    }

    // Press the matching arrow (or WASD) to teleport onto the safe spot.
    if (global.nvda_nb > 0)
    {
        var _dx = nvda_safex - x;
        var _dy = nvda_safey - y;
        var _dz = 6;
        var _go = 0;
        if ((keyboard_check_pressed(vk_left)  || keyboard_check_pressed(ord(""A""))) && _dx < (-_dz)) _go = 1;
        if ((keyboard_check_pressed(vk_right) || keyboard_check_pressed(ord(""D""))) && _dx >   _dz)  _go = 1;
        if ((keyboard_check_pressed(vk_up)    || keyboard_check_pressed(ord(""W""))) && _dy < (-_dz)) _go = 1;
        if ((keyboard_check_pressed(vk_down)  || keyboard_check_pressed(ord(""S""))) && _dy >   _dz)  _go = 1;
        if (_go == 1)
        {
            x = nvda_safex;
            y = nvda_safey;
        }
    }

    // (3) Q = speak the SOUL's 3x3 position in the box.
    if (keyboard_check_pressed(ord(""Q"")))
    {" + bridge + @"
        var _hf = (x - global.idealborder[0]) / max(1, global.idealborder[1] - global.idealborder[0]);
        var _vf = (y - global.idealborder[2]) / max(1, global.idealborder[3] - global.idealborder[2]);
        var _h = ""center"";
        if (_hf < 0.33) _h = ""left""; else if (_hf > 0.66) _h = ""right"";
        var _v = ""middle"";
        if (_vf < 0.33) _v = ""top""; else if (_vf > 0.66) _v = ""bottom"";
        external_call(global.nvda_speak, ""Heart "" + _v + "" "" + _h);
    }
}";
g.QueueAppend(Data.Code.ByName("gml_Object_obj_heart_Step_0"), safespot);

g.Import();
Console.WriteLine("Injected dodge assist v2: M toggle + HP safety net + safe-spot cue/teleport + Q locator");

}

// ===== inject_skip.csx =====
{
// Undertale accessibility - SKIP PUZZLE option (Lilian's idea), v4: DOOR PICKER.
//
// A deliberate, player-invoked LAST RESORT for puzzles that can't be done by ear.
// Appended to obj_mainchara Step_0 (overworld only) so it never fires in battle/menus.
//
// Instead of guessing an exit, you HEAR the exits and CHOOSE one:
//   Key P  -> enter skip mode: lists how many exits, announces the nearest one as
//             "Exit 1 of N, <direction>, <steps> steps."
//   Key P again -> cycle to the next exit (announced the same way). Wraps around.
//   Key O  -> GO through the currently announced exit (fires that door's own normal
//             transition: fade + room_goto to the next room's spawn point).
//   Moving -> cancels skip mode.
//
// Doors are sorted nearest-first and described by screen-direction + distance (same
// vocabulary as the nav system). A door's clean transition = its User Event 9
// (Other_19 -> 8-frame fade -> Alarm_2 room_goto), so we just fire event_user(9) on
// the chosen door - the game does the rest, dropping you on open ground next room.
//
// Generic by design: works for any room with exits, not just the bridge-seed puzzles.

foreach (var fn in new[]{
    "external_define","external_call","variable_global_exists","variable_instance_exists",
    "keyboard_check_pressed","ord","point_distance","abs","round","string","event_user"
}) Data.Functions.EnsureDefined(fn, Data.Strings);

string bridge = @"
if (!variable_global_exists(""nvda_ready""))
{
    global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
    global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
    external_call(global.nvda_init, """");
    global.nvda_ready = 1;
}";

// Announce the currently selected exit (index nvda_skipidx) relative to the player.
// _pre is a leading phrase (e.g. ""Skip mode. "" on entry, """" on cycle).
string announce = @"
var _door = global.nvda_dlid[nvda_skipidx];
var _ddx = _door.x - x;
var _ddy = _door.y - y;
var _dir = ""right"";
if (abs(_ddx) >= abs(_ddy)) { if (_ddx < 0) _dir = ""left""; else _dir = ""right""; }
else { if (_ddy < 0) _dir = ""up""; else _dir = ""down""; }
var _steps = round(point_distance(x, y, _door.x, _door.y) / 20);
external_call(global.nvda_speak, _pre + ""Exit "" + string(nvda_skipidx + 1) + "" of "" + string(global.nvda_doorn) + "", "" + _dir + "", "" + string(_steps) + "" steps."");";

string skip = @"
if (variable_global_exists(""nvda_opt_skip"") && global.nvda_opt_skip == 0) exit;
if (!variable_instance_exists(id, ""nvda_skipmode"")) nvda_skipmode = 0;
if (!variable_instance_exists(id, ""nvda_skipidx""))  nvda_skipidx = 0;

// Only active in free roam - never while a menu/dialogue/cutscene is up (interact != 0).
if (global.interact != 0)
    nvda_skipmode = 0;
// Moving cancels skip mode.
if (nvda_skipmode == 1 && (x != xprevious || y != yprevious))
{" + bridge + @"
    nvda_skipmode = 0;
    external_call(global.nvda_speak, ""Skip cancelled."");
}

// P = enter skip mode / cycle to next exit.
if (global.interact == 0 && keyboard_check_pressed(ord(""P"")))
{" + bridge + @"
    if (nvda_skipmode == 0)
    {
        // Build the exit list, sorted nearest-first.
        global.nvda_doorn = 0;
        with (obj_doorparent)
        {
            // skip ice-slide trigger tiles (not real exits) so the skip-picker isn't flooded
            if (object_index != obj_iceevent && object_index != obj_iceeventup && object_index != obj_iceeventright)
            {
                // dedupe: skip if a same-type door already added within 48px (one wide doorway)
                var _isdup = 0;
                var _q = 0;
                for (_q = 0; _q < global.nvda_doorn; _q += 1)
                {
                    var _ex = global.nvda_dlid[_q];
                    if (instance_exists(_ex) && _ex.object_index == object_index && point_distance(x, y, _ex.x, _ex.y) < 48)
                    {
                        _isdup = 1;
                        break;
                    }
                }
                if (_isdup == 0)
                {
                    global.nvda_dlid[global.nvda_doorn] = id;
                    global.nvda_dld[global.nvda_doorn] = point_distance(x, y, other.x, other.y);
                    global.nvda_doorn += 1;
                }
            }
        }
        var _i; var _j;
        for (_i = 0; _i < global.nvda_doorn - 1; _i += 1)
        {
            var _m = _i;
            for (_j = _i + 1; _j < global.nvda_doorn; _j += 1)
                if (global.nvda_dld[_j] < global.nvda_dld[_m]) _m = _j;
            if (_m != _i)
            {
                var _ti = global.nvda_dlid[_i]; global.nvda_dlid[_i] = global.nvda_dlid[_m]; global.nvda_dlid[_m] = _ti;
                var _td = global.nvda_dld[_i];  global.nvda_dld[_i]  = global.nvda_dld[_m];  global.nvda_dld[_m]  = _td;
            }
        }
        if (global.nvda_doorn <= 0)
        {
            external_call(global.nvda_speak, ""No exits found in this room."");
        }
        else
        {
            nvda_skipmode = 1;
            nvda_skipidx = 0;
            var _pre = ""Skip mode. "" + string(global.nvda_doorn) + "" exits. "";" + announce + @"
        }
    }
    else
    {
        nvda_skipidx += 1;
        if (nvda_skipidx >= global.nvda_doorn) nvda_skipidx = 0;
        var _pre = """";" + announce + @"
    }
}

// O = confirm: go through the announced exit.
if (global.interact == 0 && nvda_skipmode == 1 && keyboard_check_pressed(ord(""O"")))
{" + bridge + @"
    var _go = global.nvda_dlid[nvda_skipidx];
    nvda_skipmode = 0;
    external_call(global.nvda_speak, ""Going through exit "" + string(nvda_skipidx + 1) + ""."");
    with (_go)
        event_user(9);   // the door's own transition (fade + room_goto)
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_mainchara_Step_0"), skip);
g.Import();
Console.WriteLine("Injected SKIP PUZZLE v4: P enter/cycle exits, O confirm, move cancel -> door transition");

}

// ===== inject_menu_overworld.csx =====
{
// Undertale accessibility - OVERWORLD MENU reader (the C menu: ITEM / STAT / CELL,
// plus item actions, cell/phone, the SAVE menu, and the storage box).
//
// All of it is drawn by obj_overworldcontroller Draw_0, keyed on global.menuno while
// global.interact == 5. We append a per-frame watcher that speaks the focused entry
// whenever the menu page (global.menuno) or the cursor (global.menucoord[menuno])
// changes. The Draw event has already run this frame, so values + helper instance vars
// (nextlevel for stats; name/love/roome for save) are current when we read them.
//
//   menuno 0 = top:    menucoord[0] 0=Item 1=Stats 2=Cell
//   menuno 1/6 = items: global.itemname[menucoord]
//   menuno 5 = action:  menucoord[5] 0=Use 1=Info 2=Drop  (on the chosen item)
//   menuno 2 = stats:   read the page on entry
//   menuno 3 = cell:    global.phonename[menucoord[3]]
//   menuno 4 = save:    menucoord[4] 0=Save 1=Return 2=saved; read save info on entry
//   menuno 7 = box:     global.itemname[menucoord[7]]
// (menuno 9 = item use/info/drop RESULT = drawn by the writer -> already narrated.)

foreach (var fn in new[]{
    "external_define","external_call","variable_global_exists","variable_instance_exists","string"
}) Data.Functions.EnsureDefined(fn, Data.Strings);

string bridge = @"
if (!variable_global_exists(""nvda_ready""))
{
    global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
    global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
    external_call(global.nvda_init, """");
    global.nvda_ready = 1;
}";

string watch = @"
if (global.interact == 5)
{" + bridge + @"
    if (!variable_instance_exists(id, ""nvda_pmenu"")) { nvda_pmenu = -99; nvda_pcoord = -99; }
    var _mn = global.menuno;
    var _cc = -1;
    if (_mn >= 0 && _mn <= 7) _cc = global.menucoord[_mn];
    if (_mn != nvda_pmenu || _cc != nvda_pcoord)
    {
        var _enter = (_mn != nvda_pmenu);
        var _say = """";
        if (_mn == 0)
        {
            var _o = global.menucoord[0];
            var _nm = ""Item""; if (_o == 1) _nm = ""Stats""; if (_o == 2) _nm = ""Cell"";
            _say = _nm;
            if (_enter) _say = ""Menu. "" + _say;
        }
        else if (_mn == 1 || _mn == 6)
        {
            var _it = global.itemname[global.menucoord[_mn]];
            if (_it == """") _it = ""Empty"";
            _say = _it;
            if (_enter) _say = ""Items. "" + _say;
        }
        else if (_mn == 5)
        {
            var _a = global.menucoord[5];
            var _an = ""Use""; if (_a == 1) _an = ""Info""; if (_a == 2) _an = ""Drop"";
            _say = _an;
            if (_enter) _say = ""Choose action. "" + _say;
        }
        else if (_mn == 7)
        {
            var _it = global.itemname[global.menucoord[7]];
            if (_it == """") _it = ""Empty"";
            _say = _it;
            if (_enter) _say = ""Box. "" + _say;
        }
        else if (_mn == 3)
        {
            var _ph = global.phonename[global.menucoord[3]];
            if (_ph == """") _ph = ""Empty"";
            _say = _ph;
            if (_enter) _say = ""Cell phone. "" + _say;
        }
        else if (_mn == 2)
        {
            if (_enter)
            {
                // Read everything defensively + compute 'next level' ourselves, so we
                // never depend on the game's own 'nextlevel' instance var (reading it
                // bare can resolve to a never-set variable -> hard crash). All globals
                // guarded in case a loaded save hasn't populated one yet.
                var _nm = """"; if (variable_global_exists(""charname"")) _nm = global.charname;
                var _lv = 1;  if (variable_global_exists(""lv""))       _lv = global.lv;
                var _hp = 0;  if (variable_global_exists(""hp""))       _hp = global.hp;
                var _mhp = 0; if (variable_global_exists(""maxhp""))    _mhp = global.maxhp;
                var _at = 0;  if (variable_global_exists(""at""))       _at = global.at - 10;
                var _ws = 0;  if (variable_global_exists(""wstrength"")) _ws = global.wstrength;
                var _df = 0;  if (variable_global_exists(""df""))       _df = global.df - 10;
                var _ad = 0;  if (variable_global_exists(""adef""))     _ad = global.adef;
                var _gd = 0;  if (variable_global_exists(""gold""))     _gd = global.gold;
                var _xp = 0;  if (variable_global_exists(""xp""))       _xp = global.xp;
                var _need = 0;
                if (_lv == 1) _need = 10; else if (_lv == 2) _need = 30;
                else if (_lv == 3) _need = 70; else if (_lv == 4) _need = 120;
                else if (_lv == 5) _need = 200; else if (_lv == 6) _need = 300;
                else if (_lv == 7) _need = 500; else if (_lv == 8) _need = 800;
                else if (_lv == 9) _need = 1200; else if (_lv == 10) _need = 1700;
                else if (_lv == 11) _need = 2500; else if (_lv == 12) _need = 3500;
                else if (_lv == 13) _need = 5000; else if (_lv == 14) _need = 7000;
                else if (_lv == 15) _need = 10000; else if (_lv == 16) _need = 15000;
                else if (_lv == 17) _need = 25000; else if (_lv == 18) _need = 50000;
                else if (_lv == 19) _need = 99999; else _need = 0;
                var _nl = 0; if (_need > 0) _nl = _need - _xp;
                _say = ""Stats. "" + _nm + "". LOVE "" + string(_lv) +
                       "". HP "" + string(_hp) + "" of "" + string(_mhp) +
                       "". Attack "" + string(_at) + "" plus "" + string(_ws) +
                       "". Defense "" + string(_df) + "" plus "" + string(_ad) +
                       "". Gold "" + string(_gd) + "". Exp "" + string(_xp) +
                       "". Next "" + string(_nl) + ""."";
            }
        }
        else if (_mn == 4)
        {
            var _s = global.menucoord[4];
            if (_s == 2)
                _say = ""Saved."";
            else
            {
                var _opt = ""Save""; if (_s == 1) _opt = ""Return"";
                _say = _opt;
                if (_enter)
                    _say = ""Save point. "" + name + "". LOVE "" + string(love) + "". "" +
                           scr_roomname(roome) + "". "" + _opt + ""."";
            }
        }
        if (_say != """")
            external_call(global.nvda_speak, _say);
        nvda_pmenu = _mn;
        nvda_pcoord = _cc;
    }
}
else if (variable_instance_exists(id, ""nvda_pmenu""))
{
    nvda_pmenu = -99;   // menu closed: re-announce on next open
    nvda_pcoord = -99;
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_overworldcontroller_Draw_0"), watch);
g.Import();
Console.WriteLine("Injected OVERWORLD MENU reader (obj_overworldcontroller Draw_0): item/stat/cell/save/box");

}

// ===== inject_quiz.csx =====
{
// Undertale accessibility - METTATON QUIZ SHOW reader (Hotland).
//
// The quiz (obj_questionasker) draws its question + 4 options with raw draw_text (not
// the writer), so nothing was narrated; answers are obj_answernodule buttons that the
// soul must physically touch (which INSTANTLY submits), under a countdown (quiztimer).
// Impossible by ear. This makes it playable:
//   * Reads "Question. <qtext>. 1 A: <a1>. 2 B: <a2>. 3 C: <a3>. 4 D: <a4>." once per
//     question, and relays Alphys's hint (the correct letter) - the same help sighted
//     players get from her on-screen pose (except q7, where she's no help in-game too).
//   * Removes the time pressure (pins quiztimer) and parks the soul off the buttons so
//     it can't submit by accident.
//   * You choose with NUMBER keys 1-4 (= A B C D), hear the choice, then press Z to lock
//     it in. (Letters A/D are avoided - they're WASD movement.)
//
// Appended to obj_questionasker Draw_0 (self = the quiz; qtext/a1..a4/phase/quiztimer/
// answer/correct/mettamt are its instance vars). Nodule mapping (Other_11/Alarm_0):
// qno/ano 0=A 1=B 2=C 3=D; correct is 0-3 (or 5 = any answer counts).

foreach (var fn in new[]{
    "external_define","external_call","variable_global_exists","variable_instance_exists",
    "keyboard_check_pressed","control_check_pressed","ord","string","string_replace_all","instance_exists"
}) Data.Functions.EnsureDefined(fn, Data.Strings);

string bridge = @"
if (!variable_global_exists(""nvda_ready""))
{
    global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
    global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
    external_call(global.nvda_init, """");
    global.nvda_ready = 1;
}";

string quiz = @"
{" + bridge + @"
    if (!variable_instance_exists(id, ""nvda_qann"")) { nvda_qann = -1; nvda_pend = -1; nvda_subm = 0; }

    // Readable option texts (the numeric 'special1' question computes its own numbers).
    var _mlen = 8;
    var _oa1 = a1; if (a1 == ""special1"") _oa1 = string(mettamt + _mlen + 3);
    var _oa2 = a2; if (a2 == ""special1"") _oa2 = string((mettamt + _mlen) - 2);
    var _oa3 = a3; if (a3 == ""special1"") _oa3 = string(mettamt + _mlen);
    var _oa4 = a4; if (a4 == ""special1"") _oa4 = string(mettamt + _mlen + 2);
    var _qt = string_replace_all(qtext, ""#"", "" "");
    _qt = string_replace_all(_qt, ""&"", "" "");

    // Announce the question + options once per question.
    if (q >= 1 && phase >= 1 && phase <= 2 && nvda_qann != q)
    {
        nvda_qann = q;
        nvda_pend = -1;
        nvda_subm = 0;
        var _msg = ""Question. "" + _qt + "". 1 A: "" + _oa1 + "". 2 B: "" + _oa2 +
                   "". 3 C: "" + _oa3 + "". 4 D: "" + _oa4 + ""."";
        if (q != 7 && correct >= 0 && correct <= 3)
        {
            var _hl = ""A""; if (correct == 1) _hl = ""B""; if (correct == 2) _hl = ""C""; if (correct == 3) _hl = ""D"";
            _msg += "" Alphys hints "" + _hl + ""."";
        }
        _msg += "" Press 1 to 4, then Z to lock in."";
        external_call(global.nvda_speak, _msg);
    }

    if (phase == 2)
    {
        // Remove time pressure + keep the soul parked off the buttons (numbers choose).
        if (nvda_subm == 0)
        {
            quiztimer = 300;
            if (instance_exists(obj_heart)) { obj_heart.x = 300; obj_heart.y = 300; }
        }
        var _pick = -1;
        if (keyboard_check_pressed(ord(""1""))) _pick = 0;
        if (keyboard_check_pressed(ord(""2""))) _pick = 1;
        if (keyboard_check_pressed(ord(""3""))) _pick = 2;
        if (keyboard_check_pressed(ord(""4""))) _pick = 3;
        if (_pick != -1)
        {
            nvda_pend = _pick;
            var _lt = ""A""; var _tx = _oa1;
            if (_pick == 1) { _lt = ""B""; _tx = _oa2; }
            if (_pick == 2) { _lt = ""C""; _tx = _oa3; }
            if (_pick == 3) { _lt = ""D""; _tx = _oa4; }
            external_call(global.nvda_speak, _lt + "". "" + _tx + "". Press Z to lock in."");
        }
        if (nvda_pend != -1 && nvda_subm == 0 && control_check_pressed(0))
        {
            nvda_subm = 1;
            // correct == 5 means any answer counts; mirror that so a number pick still wins.
            if (correct == 5) answer = 5; else answer = nvda_pend;
            var _p = nvda_pend;
            with (obj_answernodule) { if (qno == _p) answered = 1; }
            phase = 3;
            var _lt2 = ""A""; if (_p == 1) _lt2 = ""B""; if (_p == 2) _lt2 = ""C""; if (_p == 3) _lt2 = ""D"";
            external_call(global.nvda_speak, ""Locked in "" + _lt2 + ""."");
        }
    }
    else
    {
        nvda_pend = -1;
        nvda_subm = 0;
    }
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_questionasker_Draw_0"), quiz);
g.Import();
Console.WriteLine("Injected METTATON QUIZ reader (obj_questionasker Draw_0): read Q+options, no timer, 1-4 + Z");

}

// ===== inject_shootguy_skip.csx =====
{
// Undertale accessibility - SHOOTING-PUZZLE skip (Hotland obj_shootguy, 5 rooms).
//
// The Hotland shooting puzzle is a visual-spatial minigame: a cannon with limited ammo
// shifts/shoots a grid of black tiles (obj_blackbox_o) to match a pattern. Genuinely
// not doable by ear. Like the bridge-seed puzzle, give a player-invoked skip.
//
// HOW IT COMPLETES NORMALLY: when obj_shootguy.win becomes 1 (and active==1), the
// cannon's own Step_0 runs a wintimer that sets the room's solved flag
// (flag 375/374/399/400/418 for rooms 1-5) and sets win=2 -> the path opens. So a clean
// skip just forces win=1 (and active=1) and lets the GAME finish the puzzle properly.
//
//   When the puzzle is active it announces the skip is available.
//   Key P -> "Skip puzzle? Press O to confirm."   Key O -> sets win=1 (auto-solves).
// Appended to obj_shootguy Step_0. (P/O are free here; the puzzle uses arrows + Z + X.)

foreach (var fn in new[]{
    "external_define","external_call","variable_global_exists","variable_instance_exists",
    "keyboard_check_pressed","ord"
}) Data.Functions.EnsureDefined(fn, Data.Strings);

string bridge = @"
if (!variable_global_exists(""nvda_ready""))
{
    global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
    global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
    external_call(global.nvda_init, """");
    global.nvda_ready = 1;
}";

string skip = @"
{
    if (variable_global_exists(""nvda_opt_skip"") && global.nvda_opt_skip == 0) exit;" + bridge + @"
    if (!variable_instance_exists(id, ""nvda_sgann"")) { nvda_sgann = 0; nvda_sgarm = 0; }

    // Announce once that this is a skippable visual puzzle, when it becomes active.
    if (active == 1 && win == 0 && nvda_sgann == 0)
    {
        nvda_sgann = 1;
        external_call(global.nvda_speak, ""Shooting puzzle. This one is visual. To skip it, press P then O."");
    }
    if (win > 0) nvda_sgann = 0;   // reset for the next puzzle room

    // P arms, O confirms -> force the win so the game's own win sequence finishes it.
    if (win == 0 && keyboard_check_pressed(ord(""P"")))
    {
        nvda_sgarm = 1;
        external_call(global.nvda_speak, ""Skip puzzle? Press O to confirm."");
    }
    if (win == 0 && nvda_sgarm == 1 && keyboard_check_pressed(ord(""O"")))
    {
        nvda_sgarm = 0;
        active = 1;
        win = 1;
        external_call(global.nvda_speak, ""Puzzle skipped. Solving."");
    }
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_shootguy_Step_0"), skip);
g.Import();
Console.WriteLine("Injected SHOOTING-PUZZLE skip (obj_shootguy Step_0): P arms, O confirms -> win=1");

}

// ===== inject_elevator.csx =====
{
// Undertale accessibility - ELEVATOR panel reader (Hotland/CORE "select a location").
//
// obj_elevatorpanel Draw_0 draws a 2-column x 3-row grid of floor buttons; the cursor
// is (heartx 0..1, hearty 0..2). The "Please select a location" prompt is a dialogue
// box (already narrated); the floor options + cursor are drawn directly (silent).
// We append a watcher that speaks the focused floor whenever the cursor moves.
//
// Cell -> gettext key (index = global.flag[398] meaning "current floor" => shows cancel):
//   (0,0) l1f #0   (1,0) r1f #1   (1,1) r2f #2
//   (0,1) l2f #3   (0,2) l3f #4   (1,2) r3f #5
// Availability is gated by `trigger`, but the panel's own movement code keeps the cursor
// on valid cells, so we just read whatever cell it is on.

foreach (var fn in new[]{
    "external_define","external_call","variable_global_exists","variable_instance_exists","string"
}) Data.Functions.EnsureDefined(fn, Data.Strings);

string bridge = @"
if (!variable_global_exists(""nvda_ready""))
{
    global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
    global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
    external_call(global.nvda_init, """");
    global.nvda_ready = 1;
}";

string watch = @"
if (con == 2 && instance_exists(OBJ_WRITER) == 0)
{" + bridge + @"
    if (!variable_instance_exists(id, ""nvda_ehx"")) { nvda_ehx = -99; nvda_ehy = -99; }
    if (heartx != nvda_ehx || hearty != nvda_ehy)
    {
        nvda_ehx = heartx;
        nvda_ehy = hearty;
        var _idx = -1; var _key = """";
        if (heartx == 0 && hearty == 0) { _idx = 0; _key = ""elevator_l1f""; }
        else if (heartx == 1 && hearty == 0) { _idx = 1; _key = ""elevator_r1f""; }
        else if (heartx == 1 && hearty == 1) { _idx = 2; _key = ""elevator_r2f""; }
        else if (heartx == 0 && hearty == 1) { _idx = 3; _key = ""elevator_l2f""; }
        else if (heartx == 0 && hearty == 2) { _idx = 4; _key = ""elevator_l3f""; }
        else if (heartx == 1 && hearty == 2) { _idx = 5; _key = ""elevator_r3f""; }
        var _label = """";
        if (_idx != -1)
        {
            if (global.flag[398] == _idx) _label = scr_gettext(""elevator_cancel"");
            else _label = scr_gettext(_key);
        }
        if (_label != """")
            external_call(global.nvda_speak, _label);
    }
}
else if (variable_instance_exists(id, ""nvda_ehx""))
{
    nvda_ehx = -99;
    nvda_ehy = -99;
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_elevatorpanel_Draw_0"), watch);
g.Import();
Console.WriteLine("Injected ELEVATOR reader (obj_elevatorpanel Draw_0): announce focused floor on cursor move");

}

// ===== inject_shop.csx =====
{
// Undertale accessibility - SHOP menu reader (obj_shop1..5).
//
// Shops share a menu state machine drawn directly (silent). The shopkeeper's dialogue
// is the writer (already narrated); the MENU options are draw_text. We append a watcher
// to each shop's Draw_0 that speaks the focused option when menu/cursor changes.
//   menu 0 = top:     menuc[0] 0=Buy 1=Sell 2=Talk 3=Exit  (Take/Steal/Read if murder)
//   menu 1 = buy:     items 0-3 (item_name_<id> + itemcost), index 4 = Exit (menuc[1])
//   menu 2 = confirm: menuc[2] 0=Yes(buy) 1=No
//   menu 3 = talk:    titles shop<N>_talk<T>_title (T=cur+1, or +5 if global.flag[7]!=0),
//                     index 4 = Exit (menuc[3]); <N> from the object name
//   menu 4 = dialogue result (writer -> already narrated; no menu speech)

foreach (var fn in new[]{
    "external_define","external_call","variable_global_exists","variable_instance_exists",
    "string","string_char_at","object_get_name"
}) Data.Functions.EnsureDefined(fn, Data.Strings);

string bridge = @"
if (!variable_global_exists(""nvda_ready""))
{
    global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
    global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
    external_call(global.nvda_init, """");
    global.nvda_ready = 1;
}";

string watch = @"
{" + bridge + @"
    if (!variable_instance_exists(id, ""nvda_smenu"")) { nvda_smenu = -99; nvda_scur = -99; }
    var _m = menu;
    var _cur = -1;
    if (_m == 0) _cur = menuc[0];
    else if (_m == 1) _cur = menuc[1];
    else if (_m == 2) _cur = menuc[2];
    else if (_m == 3) _cur = menuc[3];
    if (_m != nvda_smenu || _cur != nvda_scur)
    {
        var _enter = (_m != nvda_smenu);
        var _say = """";
        if (_m == 0)
        {
            var _o = menuc[0];
            if (_o == 0) _say = ""Buy""; else if (_o == 1) _say = ""Sell""; else if (_o == 2) _say = ""Talk""; else if (_o == 3) _say = ""Exit"";
            if (murder == 1) { if (_o == 0) _say = ""Take""; else if (_o == 1) _say = ""Steal""; else if (_o == 2) _say = ""Read""; }
            if (_enter) _say = ""Shop. "" + _say;
        }
        else if (_m == 1)
        {
            var _c = menuc[1];
            if (_c >= 0 && _c <= 3)
                _say = scr_gettext(""item_name_"" + string(item[_c])) + "", "" + string(itemcost[_c]) + "" gold"";
            else
                _say = ""Exit"";
            if (_enter) _say = ""Buy. "" + _say;
        }
        else if (_m == 2)
        {
            if (menuc[2] == 0) _say = ""Yes, buy for "" + string(itemcost[menuc[1]]) + "" gold"";
            else _say = ""No"";
            if (_enter) _say = ""Confirm. "" + _say;
        }
        else if (_m == 3)
        {
            var _c = menuc[3];
            if (_c >= 0 && _c <= 3)
            {
                var _t = _c + 1;
                if (global.flag[7] != 0) _t = _c + 5;
                var _sn = string_char_at(object_get_name(object_index), 9);
                _say = scr_gettext(""shop"" + _sn + ""_talk"" + string(_t) + ""_title"");
            }
            else _say = ""Exit"";
            if (_enter) _say = ""Talk. "" + _say;
        }
        if (_say != """")
            external_call(global.nvda_speak, _say);
        nvda_smenu = _m;
        nvda_scur = _cur;
    }
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
int n = 0;
foreach (var sh in new[]{ "obj_shop1","obj_shop2","obj_shop3","obj_shop4","obj_shop5" })
{
    var code = Data.Code.ByName("gml_Object_" + sh + "_Draw_0");
    if (code != null) { g.QueueAppend(code, watch); n++; }
    else Console.WriteLine("  (no Draw_0 for " + sh + ")");
}
g.Import();
Console.WriteLine("Injected SHOP reader into " + n + " shops (top/buy/confirm/talk)");

}

// ===== inject_flowey_souls.csx =====
{
// Undertale accessibility - PHOTOSHOP FLOWEY (neutral final boss) SOUL-RESCUE GUIDE v2.
//
// MECHANIC (verified 2026-06-26): win by raising global.soul_rescue 0->6. Each freed soul
// scales your FIGHT damage (1 -> thousands); Flowey HP 9950, so 0 souls = unwinnable.
// You free a soul by pressing Z on that soul's "_act" target. Each soul's act object has
// an Other_14 (User Event 4) that starts its rescue con-state-machine -> at con==3 it does
// global.soul_rescue += 1 and saves. The six rescue-trigger objects are:
//   obj_6knife_act (cyan), obj_6glove_act (orange), obj_6shoe_act (blue),
//   obj_6book_act (purple), obj_6pan_act (green), obj_6gun_act (yellow).
// The FIGHT button (obj_flowey_fightbt) uses the same User-Event-4 to ATTACK Flowey.
//
// v1 FAILED IN TESTING (2026-06-26): "heard a beep, centring + Z did nothing." Cause: v1
// fired event_user(4) on the NEAREST obj_centeract_parent only within 42px of its bbox
// centre -> by ear that often hit the FIGHT button (1 dmg, no feedback) or never satisfied
// the distance gate on a moving target. FIX: make Z POSITION-INDEPENDENT and context-aware
// -- during a soul round, Z frees the soul (fires User Event 4 on whichever _act exists);
// otherwise Z attacks Flowey via the fight button. The panned beep stays as orientation,
// and a spoken cue tells you which mode you're in. Diagnostic key O reads the scene.
//
// Invincibility (global.my_hp clamp) + hold-F fast-forward already live in inject_dodge.csx.

foreach (var fn in new[]{
    "external_define","external_call","variable_global_exists","variable_instance_exists",
    "instance_exists","instance_nearest","point_distance","control_check_pressed",
    "keyboard_check_pressed","ord","event_user","string"
}) Data.Functions.EnsureDefined(fn, Data.Strings);

string bridge = @"
if (!variable_global_exists(""nvda_ready""))
{
    global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
    global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
    external_call(global.nvda_init, """");
    global.nvda_ready = 1;
}";

string panbridge = @"
if (!variable_global_exists(""pan_ready""))
{
    global.pan_init = external_define(""gmpan.dll"", ""gmpan_init"", 0, 0, 0);
    global.pan_beep = external_define(""gmpan.dll"", ""gmpan_beep"", 0, 0, 4, 0, 0, 0, 0);
    external_call(global.pan_init);
    global.pan_ready = 1;
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);

// --- Soul-count watcher in persistent obj_time (catches every soul_rescue tick) ---
string srwatch = @"
if (variable_global_exists(""soul_rescue"") && instance_exists(obj_flowey_master))
{" + bridge + @"
    if (!variable_global_exists(""nvda_sr_init"") || global.nvda_sr_init == 0)
    {
        global.nvda_lastsr = global.soul_rescue;
        global.nvda_sr_init = 1;
    }
    if (global.soul_rescue > global.nvda_lastsr)
    {
        global.nvda_lastsr = global.soul_rescue;
        if (global.soul_rescue >= 6)
            external_call(global.nvda_speak, ""All six souls freed. Flowey is weak now. Press Z to attack him with the fight button."");
        else
            external_call(global.nvda_speak, ""Soul "" + string(global.soul_rescue) + "" of 6 freed. Your attacks hit harder now."");
    }
}
else if (variable_global_exists(""nvda_sr_init""))
{
    global.nvda_sr_init = 0;
}";
g.QueueAppend(Data.Code.ByName("gml_Object_obj_time_Step_1"), srwatch);

// --- Guide + context Z + auto-rescue + diagnostic, in obj_vsflowey_heart Step_0 ---
string guide = @"
if (variable_global_exists(""nvda_opt_combat"") && global.nvda_opt_combat == 0) exit;
if (variable_instance_exists(id, ""move""))
{" + bridge + panbridge + @"
    if (!variable_instance_exists(id, ""nvda_cue"")) { nvda_cue = 0; nvda_mode2 = -1; }

    // Is a soul-rescue round active? (any of the six _act trigger objects present)
    var _soul = (instance_exists(obj_6knife_act) || instance_exists(obj_6glove_act)
              || instance_exists(obj_6shoe_act) || instance_exists(obj_6book_act)
              || instance_exists(obj_6pan_act)  || instance_exists(obj_6gun_act));
    var _fbt  = instance_exists(obj_flowey_fightbt);

    // Announce mode on change.
    var _m = 0;                       // 0 = nothing actionable
    if (_soul) _m = 1;                // 1 = soul round (press Z to free)
    else if (_fbt) _m = 2;            // 2 = fight button up (press Z to attack)
    if (_m != nvda_mode2)
    {
        nvda_mode2 = _m;
        if (_m == 1) external_call(global.nvda_speak, ""Soul round. Press Z to free this soul. Follow the beep to it if you want, but Z works from anywhere."");
        else if (_m == 2) external_call(global.nvda_speak, ""Fight button up. Press Z to attack Flowey."");
    }

    // Orientation beep toward the nearest actionable target (soul act, else fight button).
    if (move == 1)
    {
        var _t = noone;
        if (_soul)
        {
            var _bd = 999999; var _c;
            _c = instance_nearest(x, y, obj_6knife_act); if (_c != noone) { var _d = point_distance(x, y, _c.x, _c.y); if (_d < _bd) { _bd = _d; _t = _c; } }
            _c = instance_nearest(x, y, obj_6glove_act); if (_c != noone) { var _d = point_distance(x, y, _c.x, _c.y); if (_d < _bd) { _bd = _d; _t = _c; } }
            _c = instance_nearest(x, y, obj_6shoe_act); if (_c != noone) { var _d = point_distance(x, y, _c.x, _c.y); if (_d < _bd) { _bd = _d; _t = _c; } }
            _c = instance_nearest(x, y, obj_6book_act); if (_c != noone) { var _d = point_distance(x, y, _c.x, _c.y); if (_d < _bd) { _bd = _d; _t = _c; } }
            _c = instance_nearest(x, y, obj_6pan_act);  if (_c != noone) { var _d = point_distance(x, y, _c.x, _c.y); if (_d < _bd) { _bd = _d; _t = _c; } }
            _c = instance_nearest(x, y, obj_6gun_act);  if (_c != noone) { var _d = point_distance(x, y, _c.x, _c.y); if (_d < _bd) { _bd = _d; _t = _c; } }
        }
        else if (_fbt)
        {
            _t = instance_nearest(x, y, obj_flowey_fightbt);
        }
        if (_t != noone)
        {
            var _hcx = x + 8; var _hcy = y + 8;
            var _tcx = (_t.bbox_left + _t.bbox_right) / 2;
            var _tcy = (_t.bbox_top + _t.bbox_bottom) / 2;
            var _dx = _tcx - _hcx; var _dy = _tcy - _hcy;
            nvda_cue += 1;
            if (nvda_cue >= 15)
            {
                nvda_cue = 0;
                var _pan = _dx / 180; if (_pan < -1) _pan = -1; if (_pan > 1) _pan = 1;
                var _nv = (0 - _dy) / 160; if (_nv < -1) _nv = -1; if (_nv > 1) _nv = 1;
                var _freq = 440 + (_nv * 280); if (_freq < 200) _freq = 200; if (_freq > 800) _freq = 800;
                external_call(global.pan_beep, _pan, _freq, 70, 0.45);
            }
        }
    }

    // Z = context action (position-independent): free the soul, or attack Flowey.
    if (control_check_pressed(0))
    {
        if (_soul)
        {
            with (obj_6knife_act) event_user(4);
            with (obj_6glove_act) event_user(4);
            with (obj_6shoe_act) event_user(4);
            with (obj_6book_act) event_user(4);
            with (obj_6pan_act)  event_user(4);
            with (obj_6gun_act)  event_user(4);
            external_call(global.nvda_speak, ""Freeing the soul."");
        }
        else if (_fbt)
        {
            with (obj_flowey_fightbt) event_user(4);
            external_call(global.nvda_speak, ""Attacking Flowey."");
        }
    }

    // R = explicit auto-free (same as Z during a soul round).
    if (keyboard_check_pressed(ord(""R"")) && _soul)
    {
        with (obj_6knife_act) event_user(4);
        with (obj_6glove_act) event_user(4);
        with (obj_6shoe_act) event_user(4);
        with (obj_6book_act) event_user(4);
        with (obj_6pan_act)  event_user(4);
        with (obj_6gun_act)  event_user(4);
        external_call(global.nvda_speak, ""Auto freeing the soul."");
    }

    // O = diagnostic: read the current scene out loud.
    if (keyboard_check_pressed(ord(""O"")))
    {
        var _sr = 0; if (variable_global_exists(""soul_rescue"")) _sr = global.soul_rescue;
        var _hp = 0; if (variable_global_exists(""my_hp"")) _hp = global.my_hp;
        var _w = ""Diagnostic. "";
        if (_soul) _w += ""Soul round active. ""; else _w += ""No soul round. "";
        if (_fbt) _w += ""Fight button present. ""; else _w += ""No fight button. "";
        _w += string(_sr) + "" of 6 souls freed. HP "" + string(_hp) + ""."";
        external_call(global.nvda_speak, _w);
    }
}";
g.QueueAppend(Data.Code.ByName("gml_Object_obj_vsflowey_heart_Step_0"), guide);

g.Import();
Console.WriteLine("Injected FLOWEY soul-rescue GUIDE v2: context-Z (position-independent) + R auto + O diagnostic + orientation beep + soul-count speech");

}


// ===== inject_choicer.csx =====
{
Data.Functions.EnsureDefined("external_define", Data.Strings);
Data.Functions.EnsureDefined("external_call", Data.Strings);
Data.Functions.EnsureDefined("variable_global_exists", Data.Strings);
Data.Functions.EnsureDefined("variable_instance_exists", Data.Strings);
Data.Functions.EnsureDefined("string_char_at", Data.Strings);
Data.Functions.EnsureDefined("string_length", Data.Strings);
Data.Functions.EnsureDefined("chr", Data.Strings);

string gml = @"
{
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }
    if (!variable_instance_exists(id, ""nvda_ch_init""))
    {
        nvda_ch_init = 1;
        nvda_lastchoice = -99;
        nvda_opt0 = """";
        nvda_opt1 = """";
        nvda_parsed = 0;
    }
    if (nvda_parsed == 0 && instance_exists(creator))
    {
        if (variable_instance_exists(creator, ""originalstring""))
        {
            var s = creator.originalstring;
            var BS = chr(92);
            var o0 = """";
            var o1 = """";

            // ---- Pass A: \>0 / \>1 position codes (Japanese) ----
            var col = -1;
            var L = string_length(s);
            var i = 1;
            while (i <= L)
            {
                var c = string_char_at(s, i);
                var step = 1;
                if (c == BS)
                {
                    var c2 = string_char_at(s, i + 1);
                    if (c2 == "">"")
                    {
                        var d = string_char_at(s, i + 2);
                        if (d == ""0"") col = 0;
                        else if (d == ""1"") col = 1;
                        else col = -1;
                        step = 3;
                    }
                    else if (c2 == ""E"" || c2 == ""F"" || c2 == ""M"" || c2 == ""T"" || c2 == ""S"" || c2 == ""z"" || c2 == ""*"")
                    {
                        step = 3;
                    }
                    else
                    {
                        step = 2;
                    }
                }
                else if (c == ""^"")
                {
                    step = 2;
                }
                else if (c == ""&"" || c == ""/"" || c == ""%"" || c == ""*"")
                {
                    step = 1;
                }
                else
                {
                    if (col == 0) o0 += c;
                    else if (col == 1) o1 += c;
                }
                i += step;
            }

            // ---- Pass B: space-positioned columns (English) ----
            if (o0 == """" || o1 == """")
            {
                o0 = """";
                o1 = """";
                var cleaned = """";
                var si = 1;
                var slen = string_length(s);
                while (si <= slen)
                {
                    var ch = string_char_at(s, si);
                    var adv = 1;
                    if (ch == BS)
                    {
                        var nx = string_char_at(s, si + 1);
                        if (nx == ""E"" || nx == ""F"" || nx == ""M"" || nx == ""T"" || nx == ""S"" || nx == ""z"" || nx == ""*"") adv = 3;
                        else adv = 2;
                    }
                    else if (ch == ""^"")
                    {
                        adv = 2;
                    }
                    else
                    {
                        cleaned += ch;
                    }
                    si += adv;
                }

                var inopt = 0;
                var line = """";
                var ci = 1;
                var clen = string_length(cleaned);
                while (ci <= (clen + 1))
                {
                    var ec = """";
                    if (ci <= clen) ec = string_char_at(cleaned, ci);
                    if (ec == ""&"" || ci > clen)
                    {
                        var llen = string_length(line);
                        var lead = 0;
                        var p = 1;
                        while (p <= llen && string_char_at(line, p) == "" "")
                        {
                            lead += 1;
                            p += 1;
                        }
                        var hasbullet = 0;
                        if (p <= llen && string_char_at(line, p) == ""*"") hasbullet = 1;
                        var t0 = """";
                        var t1 = """";
                        var tcount = 0;
                        var cur = """";
                        var srun = 0;
                        var k = 1;
                        while (k <= llen)
                        {
                            var cc = string_char_at(line, k);
                            if (cc == "" "")
                            {
                                srun += 1;
                            }
                            else
                            {
                                if (srun >= 3 && cur != """")
                                {
                                    if (tcount == 0) t0 = cur;
                                    else if (tcount == 1) t1 = cur;
                                    tcount += 1;
                                    cur = """";
                                }
                                if (srun >= 1 && srun < 3 && cur != """") cur += "" "";
                                srun = 0;
                                cur += cc;
                            }
                            k += 1;
                        }
                        if (cur != """")
                        {
                            if (tcount == 0) t0 = cur;
                            else if (tcount == 1) t1 = cur;
                            tcount += 1;
                        }
                        if (tcount >= 2)
                        {
                            inopt = 1;
                            if (o0 != """") o0 += "" "";
                            o0 += t0;
                            if (o1 != """") o1 += "" "";
                            o1 += t1;
                        }
                        else if (tcount == 1)
                        {
                            var isopt = inopt;
                            if (lead >= 8 && hasbullet == 0) isopt = 1;
                            if (isopt == 1)
                            {
                                inopt = 1;
                                if (lead >= 13)
                                {
                                    if (o1 != """") o1 += "" "";
                                    o1 += t0;
                                }
                                else
                                {
                                    if (o0 != """") o0 += "" "";
                                    o0 += t0;
                                }
                            }
                        }
                        line = """";
                    }
                    else
                    {
                        line += ec;
                    }
                    ci += 1;
                }
            }

            nvda_opt0 = o0;
            nvda_opt1 = o1;
            if (o0 != """" || o1 != """")
            {
                nvda_parsed = 1;
            }
        }
    }
    if (nvda_parsed == 1)
    {
        if (mychoice != nvda_lastchoice)
        {
            var firsttime = 0;
            if (nvda_lastchoice == -99) firsttime = 1;
            nvda_lastchoice = mychoice;
            var pick = nvda_opt0;
            var alt = nvda_opt1;
            if (mychoice == 1)
            {
                pick = nvda_opt1;
                alt = nvda_opt0;
            }
            if (firsttime == 1)
            {
                external_call(global.nvda_speak, ""Choice. "" + pick + "", or "" + alt + "". Press left or right, then Z. On "" + pick);
            }
            else
            {
                external_call(global.nvda_speak, pick);
            }
        }
    }
}";

var importGroup = new UndertaleModLib.Compiler.CodeImportGroup(Data);
importGroup.QueueAppend(Data.Code.ByName("gml_Object_obj_choicer_Step_0"), gml);
importGroup.Import();
Console.WriteLine("Injected dialogue-choice announcer into obj_choicer Step_0");

}

// ===== inject_shield.csx =====
{
Data.Functions.EnsureDefined("external_define", Data.Strings);
Data.Functions.EnsureDefined("external_call", Data.Strings);
Data.Functions.EnsureDefined("variable_global_exists", Data.Strings);
Data.Functions.EnsureDefined("variable_instance_exists", Data.Strings);
Data.Functions.EnsureDefined("point_distance", Data.Strings);
Data.Functions.EnsureDefined("abs", Data.Strings);
Data.Functions.EnsureDefined("instance_exists", Data.Strings);

string gml = @"
{
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }
    if (!variable_global_exists(""pan_ready""))
    {
        global.pan_init = external_define(""gmpan.dll"", ""gmpan_init"", 0, 0, 0);
        global.pan_beep = external_define(""gmpan.dll"", ""gmpan_beep"", 0, 0, 4, 0, 0, 0, 0);
        external_call(global.pan_init);
        global.pan_ready = 1;
    }
    if (!variable_instance_exists(id, ""nvda_sh_init""))
    {
        nvda_sh_init = 1;
        nvda_blocklast = -99;
        nvda_shtimer = 0;
        nvda_sh_intro = 0;
    }
    if (nvda_sh_intro == 0)
    {
        nvda_sh_intro = 1;
        external_call(global.nvda_speak, ""Shield. Block each spear with the arrow toward it. Beep: left or right ear is left or right, high pitch is up, low pitch is down."");
    }

    var cx = x;
    var cy = y;
    var bestd = 100000;
    var bestside = -1;
    with (obj_blockbullet)
    {
        if (x != 0 || y != 0)
        {
            var dd = point_distance(x, y, cx, cy);
            if (dd < bestd)
            {
                bestd = dd;
                var dx = x - cx;
                var dy = y - cy;
                if (abs(dx) >= abs(dy))
                {
                    if (dx < 0) bestside = 0;
                    else bestside = 1;
                }
                else
                {
                    if (dy > 0) bestside = 2;
                    else bestside = 3;
                }
            }
        }
    }
    with (obj_blockbullet2)
    {
        if (x != 0 || y != 0)
        {
            var dd2 = point_distance(x, y, cx, cy);
            if (dd2 < bestd)
            {
                bestd = dd2;
                var dx2 = x - cx;
                var dy2 = y - cy;
                if (abs(dx2) >= abs(dy2))
                {
                    if (dx2 < 0) bestside = 0;
                    else bestside = 1;
                }
                else
                {
                    if (dy2 > 0) bestside = 2;
                    else bestside = 3;
                }
            }
        }
    }

    if (nvda_shtimer > 0) nvda_shtimer -= 1;

    if (bestside == -1)
    {
        nvda_blocklast = -99;
    }
    else
    {
        if (bestside != nvda_blocklast && nvda_shtimer <= 0)
        {
            nvda_blocklast = bestside;
            nvda_shtimer = 6;
            var _pan = 0;
            var _freq = 500;
            if (bestside == 0) { _pan = -1; _freq = 440; }
            else if (bestside == 1) { _pan = 1; _freq = 440; }
            else if (bestside == 2) { _pan = 0; _freq = 300; }
            else if (bestside == 3) { _pan = 0; _freq = 750; }
            external_call(global.pan_beep, _pan, _freq, 130, 0.7);
        }
    }
}";

var importGroup = new UndertaleModLib.Compiler.CodeImportGroup(Data);
importGroup.QueueAppend(Data.Code.ByName("gml_Object_obj_spearblocker_Step_0"), gml);
importGroup.Import();
Console.WriteLine("Injected GREEN-SOUL shield spear-direction cue into obj_spearblocker Step_0");

}

// ===== IN-BATTLE / DATE heart-choice reader (papdate / adate / truechara) =====
{
// Undertale accessibility - IN-BATTLE / DATE heart-choices.
// Distinct from overworld obj_choicer (already handled): the in-battle and "date"
// (Papyrus date = obj_papdate, Alphys date = obj_adate) choices, and the final
// kill/spare pick (obj_truechara: ERASE vs DO NOT), use a per-host instance var
// pair: `choicer` (1 = picking, 2 = confirmed) + `choice` (0 = left option,
// 1 = right option).  vk_left/vk_right toggle, Z confirms.  Vanilla speaks
// nothing, so blind players can't tell what the two options are.
//
// Option text source:
//   * dates  -> global.msg[0] (space-column English layout, same as obj_choicer)
//   * truechara -> two fixed gettext labels (erase / donot)
//
// Centralised in the persistent obj_time Begin Step (runs in battle), so one hook
// covers all current and future in-battle heart-choice hosts.  One frame after the
// host sets choicer==1 we announce "Choose. <pick>, or <alt>. ..."; each arrow
// toggle re-announces the now-selected option.  Uses global state (only one choice
// is ever active at a time).

foreach (var f in new string[] {
    "external_define", "external_call", "variable_global_exists",
    "variable_instance_exists", "string_char_at", "string_length", "chr",
    "script_execute", "instance_exists"
}) Data.Functions.EnsureDefined(f, Data.Strings);

string gml = @"
{
    if (variable_global_exists(""nvda_opt_combat"") && global.nvda_opt_combat == 0) exit;
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }
    if (!variable_global_exists(""nvda_bcq_init""))
    {
        global.nvda_bcq_init = 1;
        global.nvda_bc_active = 0;
        global.nvda_bc_lastchoice = -99;
        global.nvda_bc_opt0 = """";
        global.nvda_bc_opt1 = """";
    }

    // ---- find an active in-battle heart-choice host ----
    var _bc_host = noone;
    var _bc_mode = 0;   // 0 = parse global.msg[0]; 1 = truechara fixed labels
    if (instance_exists(obj_truechara))
    {
        if (obj_truechara.choicer == 1) { _bc_host = obj_truechara; _bc_mode = 1; }
    }
    if (_bc_host == noone && instance_exists(obj_papdate))
    {
        if (obj_papdate.choicer == 1) { _bc_host = obj_papdate; _bc_mode = 0; }
    }
    if (_bc_host == noone && instance_exists(obj_adate))
    {
        if (obj_adate.choicer == 1) { _bc_host = obj_adate; _bc_mode = 0; }
    }

    if (_bc_host == noone)
    {
        global.nvda_bc_active = 0;
        global.nvda_bc_lastchoice = -99;
    }
    else
    {
        if (global.nvda_bc_active == 0)
        {
            // first appearance -> parse the two options once
            global.nvda_bc_active = 1;
            global.nvda_bc_lastchoice = -99;
            var bo0 = """";
            var bo1 = """";
            if (_bc_mode == 1)
            {
                bo0 = script_execute(scr_gettext, ""obj_truechara_erase"");
                bo1 = script_execute(scr_gettext, ""obj_truechara_donot"");
            }
            else
            {
                var bs = """";
                if (variable_global_exists(""msg"")) bs = global.msg[0];
                var BS = chr(92);

                // ---- Pass A: \>0 / \>1 position codes (Japanese) ----
                var bcol = -1;
                var bL = string_length(bs);
                var bi = 1;
                while (bi <= bL)
                {
                    var bc = string_char_at(bs, bi);
                    var bstep = 1;
                    if (bc == BS)
                    {
                        var bc2 = string_char_at(bs, bi + 1);
                        if (bc2 == "">"")
                        {
                            var bd = string_char_at(bs, bi + 2);
                            if (bd == ""0"") bcol = 0;
                            else if (bd == ""1"") bcol = 1;
                            else bcol = -1;
                            bstep = 3;
                        }
                        else if (bc2 == ""E"" || bc2 == ""F"" || bc2 == ""M"" || bc2 == ""T"" || bc2 == ""S"" || bc2 == ""z"" || bc2 == ""*"")
                        {
                            bstep = 3;
                        }
                        else
                        {
                            bstep = 2;
                        }
                    }
                    else if (bc == ""^"")
                    {
                        bstep = 2;
                    }
                    else if (bc == ""&"" || bc == ""/"" || bc == ""%"" || bc == ""*"")
                    {
                        bstep = 1;
                    }
                    else
                    {
                        if (bcol == 0) bo0 += bc;
                        else if (bcol == 1) bo1 += bc;
                    }
                    bi += bstep;
                }

                // ---- Pass B: space-positioned columns (English) ----
                if (bo0 == """" || bo1 == """")
                {
                    bo0 = """";
                    bo1 = """";
                    var bclean = """";
                    var bsi = 1;
                    var bslen = string_length(bs);
                    while (bsi <= bslen)
                    {
                        var bch = string_char_at(bs, bsi);
                        var badv = 1;
                        if (bch == BS)
                        {
                            var bnx = string_char_at(bs, bsi + 1);
                            if (bnx == ""E"" || bnx == ""F"" || bnx == ""M"" || bnx == ""T"" || bnx == ""S"" || bnx == ""z"" || bnx == ""*"") badv = 3;
                            else badv = 2;
                        }
                        else if (bch == ""^"")
                        {
                            badv = 2;
                        }
                        else
                        {
                            bclean += bch;
                        }
                        bsi += badv;
                    }

                    var binopt = 0;
                    var bline = """";
                    var bci = 1;
                    var bclen = string_length(bclean);
                    while (bci <= (bclen + 1))
                    {
                        var bec = """";
                        if (bci <= bclen) bec = string_char_at(bclean, bci);
                        if (bec == ""&"" || bci > bclen)
                        {
                            var bllen = string_length(bline);
                            var blead = 0;
                            var bp = 1;
                            while (bp <= bllen && string_char_at(bline, bp) == "" "")
                            {
                                blead += 1;
                                bp += 1;
                            }
                            var bbullet = 0;
                            if (bp <= bllen && string_char_at(bline, bp) == ""*"") bbullet = 1;
                            var bt0 = """";
                            var bt1 = """";
                            var btc = 0;
                            var bcur = """";
                            var brun = 0;
                            var bk = 1;
                            while (bk <= bllen)
                            {
                                var bcc = string_char_at(bline, bk);
                                if (bcc == "" "")
                                {
                                    brun += 1;
                                }
                                else
                                {
                                    if (brun >= 3 && bcur != """")
                                    {
                                        if (btc == 0) bt0 = bcur;
                                        else if (btc == 1) bt1 = bcur;
                                        btc += 1;
                                        bcur = """";
                                    }
                                    if (brun >= 1 && brun < 3 && bcur != """") bcur += "" "";
                                    brun = 0;
                                    bcur += bcc;
                                }
                                bk += 1;
                            }
                            if (bcur != """")
                            {
                                if (btc == 0) bt0 = bcur;
                                else if (btc == 1) bt1 = bcur;
                                btc += 1;
                            }
                            if (btc >= 2)
                            {
                                binopt = 1;
                                if (bo0 != """") bo0 += "" "";
                                bo0 += bt0;
                                if (bo1 != """") bo1 += "" "";
                                bo1 += bt1;
                            }
                            else if (btc == 1)
                            {
                                var bisopt = binopt;
                                if (blead >= 8 && bbullet == 0) bisopt = 1;
                                if (bisopt == 1)
                                {
                                    binopt = 1;
                                    if (blead >= 13)
                                    {
                                        if (bo1 != """") bo1 += "" "";
                                        bo1 += bt0;
                                    }
                                    else
                                    {
                                        if (bo0 != """") bo0 += "" "";
                                        bo0 += bt0;
                                    }
                                }
                            }
                            bline = """";
                        }
                        else
                        {
                            bline += bec;
                        }
                        bci += 1;
                    }
                }
            }
            global.nvda_bc_opt0 = bo0;
            global.nvda_bc_opt1 = bo1;
        }

        var bsel = _bc_host.choice;
        if (bsel != global.nvda_bc_lastchoice)
        {
            var bfirst = 0;
            if (global.nvda_bc_lastchoice == -99) bfirst = 1;
            global.nvda_bc_lastchoice = bsel;
            var bpick = global.nvda_bc_opt0;
            var balt = global.nvda_bc_opt1;
            if (bsel == 1)
            {
                bpick = global.nvda_bc_opt1;
                balt = global.nvda_bc_opt0;
            }
            if (bpick == """") bpick = ""option"";
            if (balt == """") balt = ""option"";
            if (bfirst == 1)
            {
                external_call(global.nvda_speak, ""Choose. "" + bpick + "", or "" + balt + "". Press left or right, then Z. On "" + bpick);
            }
            else
            {
                external_call(global.nvda_speak, bpick);
            }
        }
    }
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_time_Step_1"), gml);
g.Import();
Console.WriteLine("Injected IN-BATTLE/DATE heart-choice reader into obj_time Step_1");

}

// ===== ASGORE final kill/spare choice (obj_anybt + obj_fakeheart) =====
{
// Undertale accessibility - ASGORE final KILL / SPARE choice (and the reused
// Flowey post-Asgore button choice).
// When Asgore's HP drops to <=500, obj_asgoreb spawns obj_asgore_lastcutscene,
// which (con 10->11) creates a FREE-MOVING soul (obj_fakeheart) and two button
// boxes (obj_anybt): type 0 = FIGHT/kill on the LEFT, type 1 = SPARE on the RIGHT.
// You move the soul (arrows / WASD via obj_time dirs) onto a box; touching it sets
// the box's `on` flag (2-frame, re-set each overlap frame), and Z while on it fires
// event_user(0) = the kill/spare outcome.  Vanilla gives NO audio, so a blind
// player can't tell where the boxes are or which one the soul is over.
//
// Fix (deliberately faithful - she still moves the soul + presses Z herself, no
// one-key kill that could wreck a pacifist run):
//   1. obj_anybt Create  -> one-shot spoken intro on the first box ("Kill left,
//      spare right, move onto a box and press Z").
//   2. obj_anybt Step    -> speak the box label ("Kill"/"Spare") the moment the
//      soul enters it (on goes <=0 -> >0), so she knows what she's on -> press Z.
//   3. obj_fakeheart Step -> a gmpan panned beep toward the nearest box (pan =
//      left/right, pitch = up/down) so she can locate/approach them by ear.
// obj_anybt is also reused in the post-Asgore Flowey scene (type 2/3); labelled
// generically there.

foreach (var f in new string[] {
    "external_define","external_call","variable_global_exists","variable_instance_exists",
    "instance_exists","instance_number","instance_nearest"
}) Data.Functions.EnsureDefined(f, Data.Strings);

string nvdaBridge = @"
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }";

string panBridge = @"
    if (!variable_global_exists(""pan_ready""))
    {
        global.pan_init = external_define(""gmpan.dll"", ""gmpan_init"", 0, 0, 0);
        global.pan_beep = external_define(""gmpan.dll"", ""gmpan_beep"", 0, 0, 4, 0, 0, 0, 0);
        external_call(global.pan_init);
        global.pan_ready = 1;
    }";

// ---- 1+2: obj_anybt Create (intro) + Step (label on entry) ----
string createGml = @"
{" + nvdaBridge + @"
    if (instance_number(obj_anybt) == 1)
    {
        var _intro = ""Choose. Move your heart onto a box and press Z. You will hear each box as you reach it."";
        if (type == 0) _intro = ""You can kill or spare him. Kill is on the left. Spare is on the right. Move your heart onto a box, then press Z."";
        external_call(global.nvda_speak, _intro);
    }
}";

string stepGml = @"
{" + nvdaBridge + @"
    if (!variable_instance_exists(id, ""nvda_bt_set""))
    {
        nvda_bt_set = 1;
        nvda_laston = 0;
        nvda_lbl = ""Box"";
        if (type == 0) nvda_lbl = ""Kill"";
        else if (type == 1) nvda_lbl = ""Spare"";
        else if (type == 2) nvda_lbl = ""Fight"";
        else if (type == 3) nvda_lbl = ""Spare"";
    }
    if (on > 0 && nvda_laston <= 0)
    {
        external_call(global.nvda_speak, nvda_lbl + "". Press Z."");
    }
    nvda_laston = on;
}";

// ---- 3: obj_fakeheart Step (panned locate beep) ----
string fhGml = @"
{" + nvdaBridge + panBridge + @"
    if (!variable_global_exists(""nvda_fh_init""))
    {
        global.nvda_fh_init = 1;
        global.nvda_fh_timer = 0;
    }
    if (global.nvda_fh_timer > 0) global.nvda_fh_timer -= 1;
    if (movement == 1 && instance_exists(obj_anybt) && global.nvda_fh_timer <= 0)
    {
        var _t = instance_nearest(x, y, obj_anybt);
        if (_t != noone)
        {
            var _cx = _t.x + 16;
            var _cy = _t.y + 10;
            var _dx = _cx - x;
            var _dy = _cy - y;
            var _pan = _dx / 120;
            if (_pan > 1) _pan = 1;
            if (_pan < -1) _pan = -1;
            var _freq = 500 - (_dy * 3);
            if (_freq < 250) _freq = 250;
            if (_freq > 800) _freq = 800;
            external_call(global.pan_beep, _pan, _freq, 70, 0.5);
            global.nvda_fh_timer = 12;
        }
    }
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_anybt_Create_0"), createGml);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_anybt_Step_0"), stepGml);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_fakeheart_Step_0"), fhGml);
g.Import();
Console.WriteLine("Injected ASGORE kill/spare choice reader (obj_anybt + obj_fakeheart)");

}

// ===== inject_sounds.csx : NAVIGATION SOUND EFFECTS (footsteps / wall-bump / door / interact ping) =====
{
string[] need = { "audio_play_sound","audio_sound_gain","audio_sound_pitch",
                  "variable_instance_exists","variable_global_exists",
                  "instance_nearest","instance_exists","point_distance","abs",
                  "collision_rectangle" };
foreach (var f in need) Data.Functions.EnsureDefined(f, Data.Strings);

string createGml = @"
{
    nvda_footcd  = 0;
    nvda_footflip = 0;
    nvda_bumpcd  = 0;
    nvda_doorsnd = 0;
    nvda_rangeid = noone;
}";

string stepEndGml = @"
{
    if (variable_global_exists(""nvda_opt_navsfx"") && global.nvda_opt_navsfx == 0) exit;
    var FOOT_SOUND    = snd_undynestep;
    var FOOT_GAIN     = 0.35;
    var FOOT_INTERVAL = 14;
    var FOOT_PITCH_A  = 0.85;
    var FOOT_PITCH_B  = 1.0;
    var BUMP_SOUND    = snd_hit;
    var BUMP_GAIN     = 0.45;
    var BUMP_CD       = 18;

    if (!variable_instance_exists(id, ""nvda_footcd"")) { nvda_footcd = 0; nvda_footflip = 0; nvda_bumpcd = 0; }

    var _free = (movement == 1 && global.interact == 0);
    var _moved = (x != xprevious || y != yprevious);
    var _tried = (obj_time.up || obj_time.down || obj_time.left || obj_time.right);

    if (_free && _moved)
    {
        if (nvda_footcd <= 0)
        {
            var _s = audio_play_sound(FOOT_SOUND, 30, false);
            audio_sound_gain(_s, FOOT_GAIN, 0);
            if (nvda_footflip == 0) { audio_sound_pitch(_s, FOOT_PITCH_A); nvda_footflip = 1; }
            else { audio_sound_pitch(_s, FOOT_PITCH_B); nvda_footflip = 0; }
            nvda_footcd = FOOT_INTERVAL;
        }
    }
    else
    {
        nvda_footcd = 0;
    }
    if (nvda_footcd > 0) nvda_footcd -= 1;

    if (_free && _tried && !_moved)
    {
        if (nvda_bumpcd <= 0)
        {
            var _b = audio_play_sound(BUMP_SOUND, 30, false);
            audio_sound_gain(_b, BUMP_GAIN, 0);
            nvda_bumpcd = BUMP_CD;
        }
    }
    if (nvda_bumpcd > 0) nvda_bumpcd -= 1;

    var DOOR_SOUND = snd_bigdoor_open;
    var DOOR_GAIN  = 0.55;
    if (!variable_instance_exists(id, ""nvda_doorsnd"")) nvda_doorsnd = 0;
    var _door = collision_rectangle(bbox_left, bbox_top, bbox_right, bbox_bottom, obj_doorparent, 0, 0);
    if (_door != noone)
    {
        if (nvda_doorsnd == 0)
        {
            var _d = audio_play_sound(DOOR_SOUND, 40, false);
            audio_sound_gain(_d, DOOR_GAIN, 0);
            nvda_doorsnd = 1;
        }
    }
    else nvda_doorsnd = 0;

    var INT_SOUND   = snd_bell;
    var INT_GAIN    = 0.45;
    var INT_RANGE   = 24;
    var INT_RELEASE = 36;
    if (!variable_instance_exists(id, ""nvda_rangeid"")) nvda_rangeid = noone;
    if (global.interact == 0)
    {
        var _near = instance_nearest(x, y, obj_interactable);
        if (_near != noone && instance_exists(_near))
        {
            var _nd = point_distance(x, y, _near.x, _near.y);
            if (_nd <= INT_RANGE)
            {
                if (_near != nvda_rangeid)
                {
                    var _ic = audio_play_sound(INT_SOUND, 40, false);
                    audio_sound_gain(_ic, INT_GAIN, 0);
                    nvda_rangeid = _near;
                }
            }
            else if (nvda_rangeid != noone && _nd > INT_RELEASE)
            {
                nvda_rangeid = noone;
            }
        }
        else nvda_rangeid = noone;
    }
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_mainchara_Create_0"), createGml);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_mainchara_Step_2"), stepEndGml);
g.Import();
Console.WriteLine("Injected navigation sound effects: footsteps + wall-bump + door + interact ping");
}

// ===== inject_musicvol.csx : MUSIC VOLUME CONTROL (N quieter / B louder, spoken, default 50%) =====
{
string[] need = { "external_define","external_call","variable_global_exists",
                  "keyboard_check_pressed","ord","audio_sound_gain","audio_sound_get_gain",
                  "is_real","round","string","audio_is_playing",
                  "ds_list_create","ds_list_add","ds_list_size","ds_list_find_value","ds_list_delete" };
foreach (var f in need) Data.Functions.EnsureDefined(f, Data.Strings);

string bridge = @"
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }";

string gml = @"
{" + bridge + @"
    if (!variable_global_exists(""nvda_musicvol"")) global.nvda_musicvol = 0.5;
    if (!variable_global_exists(""nvda_mtrack"")) global.nvda_mtrack = ds_list_create();

    var _changed = 0;
    if (keyboard_check_pressed(ord(""N""))) { global.nvda_musicvol -= 0.1; _changed = 1; }
    if (keyboard_check_pressed(ord(""B""))) { global.nvda_musicvol += 0.1; _changed = 1; }
    if (global.nvda_musicvol < 0) global.nvda_musicvol = 0;
    if (global.nvda_musicvol > 1) global.nvda_musicvol = 1;

    // Walk the tracked-music list (filled by the caster_* hooks below - it catches TITLE and
    // COMBAT music, which never set global.currentsong). Drop finished instances; on a key press
    // snap each playing track straight to the new level (so louder is instant too); otherwise
    // hold each track at or below the ceiling (game fade-ins may push it back up).
    var _li = ds_list_size(global.nvda_mtrack) - 1;
    while (_li >= 0)
    {
        var _sid = ds_list_find_value(global.nvda_mtrack, _li);
        if (!audio_is_playing(_sid))
            ds_list_delete(global.nvda_mtrack, _li);
        else if (_changed)
            audio_sound_gain(_sid, global.nvda_musicvol, 0);
        else if (audio_sound_get_gain(_sid) > (global.nvda_musicvol + 0.001))
            audio_sound_gain(_sid, global.nvda_musicvol, 0);
        _li -= 1;
    }

    // Also clamp global.currentsong directly (overworld area music started before the tracker existed).
    var _have = 0;
    var _cs = 0;
    if (variable_global_exists(""currentsong""))
    {
        _cs = global.currentsong;
        if (is_real(_cs) && _cs > 0) _have = 1;
    }
    if (_changed && _have) audio_sound_gain(_cs, global.nvda_musicvol, 0);
    else if (_have && audio_sound_get_gain(_cs) > (global.nvda_musicvol + 0.001))
        audio_sound_gain(_cs, global.nvda_musicvol, 0);

    if (_changed)
    {
        var _pct = round(global.nvda_musicvol * 100);
        if (_pct <= 0) external_call(global.nvda_speak, ""Music off"");
        else external_call(global.nvda_speak, ""Music "" + string(_pct) + "" percent"");
    }
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_time_Step_1"), gml);

// Record every played music instance into global.nvda_mtrack so the volume control can reach
// TITLE and COMBAT music too. ALL music in this port flows through these 3 caster scripts
// (caster_play / caster_play_l / caster_loop), each ending in `return this_song_i;`.
g.ThrowOnNoOpFindReplace = true;
string trackLine = "if (variable_global_exists(\"nvda_mtrack\")) ds_list_add(global.nvda_mtrack, this_song_i);\nreturn this_song_i;";
g.QueueRegexFindReplace(Data.Code.ByName("gml_Script_caster_play"),   @"return this_song_i;", trackLine);
g.QueueRegexFindReplace(Data.Code.ByName("gml_Script_caster_play_l"), @"return this_song_i;", trackLine);
g.QueueRegexFindReplace(Data.Code.ByName("gml_Script_caster_loop"),   @"return this_song_i;", trackLine);
g.Import();
Console.WriteLine("Injected music volume control (N quieter / B louder, default 50%) + caster music tracking");
}

// Shared accessibility-menu DRIVE logic (open/close + navigate + toggle + announce). Injected
// into BOTH obj_time Step_1 (drives it in the overworld) AND scr_namingscreen (drives it at the
// title screen, where obj_time's appended code does not reliably run). Exactly one driver runs
// at a time: obj_time gates this on !instance_exists(obj_intromenu); the title script only runs
// while obj_intromenu exists. Uses only globals + keyboard, so it is context-independent (the
// up/down/left/right=0 movement-consume is harmless when run inside obj_intromenu).
string menuDrive = @"
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }
    var _announce = 0;
    var _pre = """";
    var _maxsel = 6;
    var _canopen = (variable_global_exists(""interact"") && global.interact == 0
                    && instance_exists(obj_mainchara) && !instance_exists(obj_battlecontroller));
    if (!variable_global_exists(""nvda_menu_wasopen"")) global.nvda_menu_wasopen = 0;
    if (keyboard_check_pressed(ord(""K"")))
    {
        if (global.nvda_menu_open == 1) global.nvda_menu_open = 0;
        else if (_canopen) global.nvda_menu_open = 1;
    }
    if (global.nvda_menu_open == 1 && control_check_pressed(1))
        global.nvda_menu_open = 0;
    if (global.nvda_menu_open == 1 && global.nvda_menu_wasopen == 0)
    {
        global.nvda_menu_sel = 0;
        _announce = 1;
        _pre = ""Accessibility menu. Up and down to move, left and right to change. Press K or cancel to close. "";
    }
    if (global.nvda_menu_open == 0 && global.nvda_menu_wasopen == 1)
    {
        if (instance_exists(obj_mainchara) && !instance_exists(obj_intromenu) && variable_global_exists(""interact"")) global.interact = 0;
        external_call(global.nvda_speak, ""Accessibility menu closed."");
    }
    if (global.nvda_menu_open == 1)
    {
        if (instance_exists(obj_mainchara) && !instance_exists(obj_intromenu) && variable_global_exists(""interact"")) global.interact = 99;
        up = 0; down = 0; left = 0; right = 0;
        if (keyboard_check_pressed(vk_up))   { global.nvda_menu_sel -= 1; _announce = 1; }
        if (keyboard_check_pressed(vk_down)) { global.nvda_menu_sel += 1; _announce = 1; }
        if (global.nvda_menu_sel < 0) global.nvda_menu_sel = _maxsel;
        if (global.nvda_menu_sel > _maxsel) global.nvda_menu_sel = 0;
        var _s = global.nvda_menu_sel;
        var _lr = 0;
        if (keyboard_check_pressed(vk_right)) _lr = 1;
        else if (keyboard_check_pressed(vk_left)) _lr = -1;
        if (_lr != 0)
        {
            if (_s == 0) global.nvda_opt_speech = 1 - global.nvda_opt_speech;
            else if (_s == 1) global.nvda_opt_scan = 1 - global.nvda_opt_scan;
            else if (_s == 2) global.nvda_opt_skip = 1 - global.nvda_opt_skip;
            else if (_s == 3) global.nvda_opt_navsfx = 1 - global.nvda_opt_navsfx;
            else if (_s == 4) global.nvda_opt_combat = 1 - global.nvda_opt_combat;
            else if (_s == 5)
            {
                global.nvda_mode += _lr;
                if (global.nvda_mode < 0) global.nvda_mode = 2;
                if (global.nvda_mode > 2) global.nvda_mode = 0;
                global.nvda_assist = (global.nvda_mode == 0);
            }
            else if (_s == 6)
            {
                global.nvda_musicvol += (_lr * 0.1);
                if (global.nvda_musicvol < 0) global.nvda_musicvol = 0;
                if (global.nvda_musicvol > 1) global.nvda_musicvol = 1;
                if (variable_global_exists(""currentsong"") && is_real(global.currentsong) && global.currentsong > 0)
                    audio_sound_gain(global.currentsong, global.nvda_musicvol, 0);
            }
            _announce = 1;
            global.nvda_opt_dirty = 1;
        }
    }
    if (_announce == 1 && global.nvda_menu_open == 1)
    {
        var _s2 = global.nvda_menu_sel;
        var _lbl = """";
        var _val = """";
        if (_s2 == 0) { _lbl = ""Screen reading""; if (global.nvda_opt_speech == 1) _val = ""on""; else _val = ""off""; }
        else if (_s2 == 1) { _lbl = ""Object scanning and guidance""; if (global.nvda_opt_scan == 1) _val = ""on""; else _val = ""off""; }
        else if (_s2 == 2) { _lbl = ""Puzzle skipping""; if (global.nvda_opt_skip == 1) _val = ""on""; else _val = ""off""; }
        else if (_s2 == 3) { _lbl = ""Navigation sounds""; if (global.nvda_opt_navsfx == 1) _val = ""on""; else _val = ""off""; }
        else if (_s2 == 4) { _lbl = ""Combat cues""; if (global.nvda_opt_combat == 1) _val = ""on""; else _val = ""off""; }
        else if (_s2 == 5)
        {
            _lbl = ""Assist mode"";
            if (global.nvda_mode == 0) _val = ""Assisted, you cannot be defeated"";
            else if (global.nvda_mode == 1) _val = ""Slow, fights run at half speed"";
            else _val = ""Normal, you can be defeated"";
        }
        else if (_s2 == 6) { _lbl = ""Music volume""; _val = string(round(global.nvda_musicvol * 100)) + "" percent""; }
        external_call(global.nvda_speak, _pre + _lbl + "", "" + _val);
    }
    global.nvda_menu_wasopen = global.nvda_menu_open;
";

// ===== inject_accessmenu.csx : ACCESSIBILITY OPTIONS MENU (K opens; master control panel) =====
// One spoken menu to toggle every feature: screen reading, object scanning, puzzle
// skipping, navigation sounds, combat cues, plus assist mode and music volume.
// Hosted in obj_time Begin Step (persistent). Opens only in overworld free-roam.
// Freezes the player via global.interact = 99 (a value no other system reacts to, so it
// does NOT trigger the C menu at 5; restored to 0 on close). Up/down move, left/right
// change, K closes. Settings persist in nvda_access.ini (GameMaker save area).
// Option globals (default 1 = on) are READ by guards added to each feature block; a guard
// treats a missing global as ON, so features behave normally until first loaded/toggled.
{
string[] need = { "external_define","external_call","variable_global_exists",
                  "keyboard_check_pressed","ord","instance_exists","is_real","round","string",
                  "audio_sound_gain","ini_open","ini_close","ini_read_real","ini_write_real",
                  "ini_read_string","ini_write_string",
                  "control_check_pressed" };
foreach (var f in need) Data.Functions.EnsureDefined(f, Data.Strings);

string bridge = @"
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }";

string gml = @"
{" + bridge + @"
    // ---- one-time load of saved options from ini (defaults = on) ----
    if (!variable_global_exists(""nvda_opt_loaded""))
    {
        ini_open(""nvda_access.ini"");
        global.nvda_opt_speech = ini_read_real(""options"", ""speech"", 1);
        global.nvda_opt_scan   = ini_read_real(""options"", ""scan"",   1);
        global.nvda_opt_skip   = ini_read_real(""options"", ""skip"",   1);
        global.nvda_opt_navsfx = ini_read_real(""options"", ""navsfx"", 1);
        global.nvda_opt_combat = ini_read_real(""options"", ""combat"", 1);
        global.nvda_mode       = ini_read_real(""options"", ""mode"",   0);
        global.nvda_musicvol   = ini_read_real(""options"", ""music"",  0.5);
        global.nvda_seen_chars = ini_read_string(""descriptions"", ""seen"", ""|"");
        global.nvda_cs_seen = ini_read_string(""descriptions"", ""cutscenes"", ""|"");
        global.nvda_amb_seen = ini_read_string(""descriptions"", ""ambiance"", ""|"");
        ini_close();
        global.nvda_assist = (global.nvda_mode == 0);
        global.nvda_menu_open = 0;
        global.nvda_menu_sel = 0;
        global.nvda_menu_wasopen = 0;
        global.nvda_opt_dirty = 0;
        global.nvda_opt_loaded = 1;
    }

    // Flush changed options to ini ONLY in the overworld (obj_mainchara present). The title
    // menu (scr_namingscreen) keeps undertale.ini perpetually OPEN and GameMaker allows only
    // ONE ini open at a time, so writing nvda_access.ini there corrupts the game's ini and
    // breaks the title menu. Defer the disk write until we're safely in normal gameplay.
    if (variable_global_exists(""nvda_opt_dirty"") && global.nvda_opt_dirty == 1
        && instance_exists(obj_mainchara) && !instance_exists(obj_intromenu))
    {
        ini_open(""nvda_access.ini"");
        ini_write_real(""options"", ""speech"", global.nvda_opt_speech);
        ini_write_real(""options"", ""scan"",   global.nvda_opt_scan);
        ini_write_real(""options"", ""skip"",   global.nvda_opt_skip);
        ini_write_real(""options"", ""navsfx"", global.nvda_opt_navsfx);
        ini_write_real(""options"", ""combat"", global.nvda_opt_combat);
        ini_write_real(""options"", ""mode"",   global.nvda_mode);
        ini_write_real(""options"", ""music"",  global.nvda_musicvol);
        if (variable_global_exists(""nvda_seen_chars"")) ini_write_string(""descriptions"", ""seen"", global.nvda_seen_chars);
        if (variable_global_exists(""nvda_cs_seen"")) ini_write_string(""descriptions"", ""cutscenes"", global.nvda_cs_seen);
        if (variable_global_exists(""nvda_amb_seen"")) ini_write_string(""descriptions"", ""ambiance"", global.nvda_amb_seen);
        ini_close();
        global.nvda_opt_dirty = 0;
    }

    // DRIVE the menu here ONLY in the overworld; at the title (obj_intromenu present) the
    // scr_namingscreen prepend driver runs it instead. obj_time Step_1 does NOT reliably run
    // at room_intromenu, so driving the title menu from obj_time froze it (undriven overlay).
    if (!instance_exists(obj_intromenu))
    {
    var _announce = 0;
    var _pre = """";
    var _maxsel = 6;

    var _canopen = (variable_global_exists(""interact"") && global.interact == 0
                    && instance_exists(obj_mainchara) && !instance_exists(obj_battlecontroller));

    if (!variable_global_exists(""nvda_menu_wasopen"")) global.nvda_menu_wasopen = 0;

    // ---- open / close. K toggles (open only in overworld free-roam); X closes; the
    // TITLE menu can also open it by setting global.nvda_menu_open = 1 itself. ----
    if (keyboard_check_pressed(ord(""K"")))
    {
        if (global.nvda_menu_open == 1) global.nvda_menu_open = 0;
        else if (_canopen) global.nvda_menu_open = 1;
    }
    if (global.nvda_menu_open == 1 && control_check_pressed(1))   // X = cancel = close
        global.nvda_menu_open = 0;

    // edge: just opened (via K here OR by the title-menu selection) -> announce intro
    if (global.nvda_menu_open == 1 && global.nvda_menu_wasopen == 0)
    {
        global.nvda_menu_sel = 0;
        _announce = 1;
        _pre = ""Accessibility menu. Up and down to move, left and right to change. Press K or cancel to close. "";
    }
    // edge: just closed -> unfreeze the player (if any) and confirm
    if (global.nvda_menu_open == 0 && global.nvda_menu_wasopen == 1)
    {
        if (instance_exists(obj_mainchara) && !instance_exists(obj_intromenu) && variable_global_exists(""interact"")) global.interact = 0;
        external_call(global.nvda_speak, ""Accessibility menu closed."");
    }

    // ---- while open: drive the menu, freeze the player if there is one ----
    if (global.nvda_menu_open == 1)
    {
        if (instance_exists(obj_mainchara) && !instance_exists(obj_intromenu) && variable_global_exists(""interact"")) global.interact = 99;
        up = 0; down = 0; left = 0; right = 0;   // consume movement (we are obj_time)

        if (keyboard_check_pressed(vk_up))   { global.nvda_menu_sel -= 1; _announce = 1; }
        if (keyboard_check_pressed(vk_down)) { global.nvda_menu_sel += 1; _announce = 1; }
        if (global.nvda_menu_sel < 0) global.nvda_menu_sel = _maxsel;
        if (global.nvda_menu_sel > _maxsel) global.nvda_menu_sel = 0;

        var _s = global.nvda_menu_sel;
        var _lr = 0;
        if (keyboard_check_pressed(vk_right)) _lr = 1;
        else if (keyboard_check_pressed(vk_left)) _lr = -1;

        if (_lr != 0)
        {
            if (_s == 0) global.nvda_opt_speech = 1 - global.nvda_opt_speech;
            else if (_s == 1) global.nvda_opt_scan = 1 - global.nvda_opt_scan;
            else if (_s == 2) global.nvda_opt_skip = 1 - global.nvda_opt_skip;
            else if (_s == 3) global.nvda_opt_navsfx = 1 - global.nvda_opt_navsfx;
            else if (_s == 4) global.nvda_opt_combat = 1 - global.nvda_opt_combat;
            else if (_s == 5)
            {
                global.nvda_mode += _lr;
                if (global.nvda_mode < 0) global.nvda_mode = 2;
                if (global.nvda_mode > 2) global.nvda_mode = 0;
                global.nvda_assist = (global.nvda_mode == 0);
            }
            else if (_s == 6)
            {
                global.nvda_musicvol += (_lr * 0.1);
                if (global.nvda_musicvol < 0) global.nvda_musicvol = 0;
                if (global.nvda_musicvol > 1) global.nvda_musicvol = 1;
                if (variable_global_exists(""currentsong"") && is_real(global.currentsong) && global.currentsong > 0)
                    audio_sound_gain(global.currentsong, global.nvda_musicvol, 0);
            }
            _announce = 1;
            global.nvda_opt_dirty = 1;   // mark dirty; the deferred flush above writes it
                                         // safely (never while the game holds its ini open)
        }
    }

    // ---- speak the focused option + its current value ----
    if (_announce == 1 && global.nvda_menu_open == 1)
    {
        var _s2 = global.nvda_menu_sel;
        var _lbl = """";
        var _val = """";
        if (_s2 == 0) { _lbl = ""Screen reading""; if (global.nvda_opt_speech == 1) _val = ""on""; else _val = ""off""; }
        else if (_s2 == 1) { _lbl = ""Object scanning and guidance""; if (global.nvda_opt_scan == 1) _val = ""on""; else _val = ""off""; }
        else if (_s2 == 2) { _lbl = ""Puzzle skipping""; if (global.nvda_opt_skip == 1) _val = ""on""; else _val = ""off""; }
        else if (_s2 == 3) { _lbl = ""Navigation sounds""; if (global.nvda_opt_navsfx == 1) _val = ""on""; else _val = ""off""; }
        else if (_s2 == 4) { _lbl = ""Combat cues""; if (global.nvda_opt_combat == 1) _val = ""on""; else _val = ""off""; }
        else if (_s2 == 5)
        {
            _lbl = ""Assist mode"";
            if (global.nvda_mode == 0) _val = ""Assisted, you cannot be defeated"";
            else if (global.nvda_mode == 1) _val = ""Slow, fights run at half speed"";
            else _val = ""Normal, you can be defeated"";
        }
        else if (_s2 == 6) { _lbl = ""Music volume""; _val = string(round(global.nvda_musicvol * 100)) + "" percent""; }
        external_call(global.nvda_speak, _pre + _lbl + "", "" + _val);
    }

    global.nvda_menu_wasopen = global.nvda_menu_open;
    }
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_time_Step_1"), gml);
g.Import();
Console.WriteLine("Injected accessibility options menu (K opens; 7 options; ini-persisted)");
}

// ===== TITLE-SCREEN ACCESSIBILITY OPTION: add a 4th item to scr_namingscreen's menu =====
// naming==3 is the load/title menu. We add one more cursor position:
//   with a save:    Continue(0) / Reset(1) / Settings(2) / Accessibility(3)
//   without a save: Begin Game(0) / Settings(1) / Accessibility(2)
// Down/up reach the new item (the game clears the arrow keys here, so we MUST extend the
// game's own nav handlers, not just append). Confirming on it opens the accessibility menu
// (the naming announcer append sets global.nvda_menu_open=1 on that action; obj_time drives
// it). While the overlay is open we PREPEND an early-exit so the title script doesn't move
// its own cursor from the same arrow presses.
{
    foreach (var f in new string[] { "variable_global_exists", "external_define", "external_call",
             "keyboard_check_pressed", "ord", "control_check_pressed", "is_real", "round", "string",
             "audio_sound_gain" })
        Data.Functions.EnsureDefined(f, Data.Strings);
    var gt = new UndertaleModLib.Compiler.CodeImportGroup(Data);
    gt.ThrowOnNoOpFindReplace = true;
    var nm = Data.Code.ByName("gml_Script_scr_namingscreen");

    // (a) DRIVE the accessibility overlay from INSIDE scr_namingscreen, which provably runs
    // every frame at the title (it reads the title arrow keys). obj_time Step_1 does NOT run
    // at room_intromenu, so the overlay must be driven here. While open we drive it then exit
    // (so the title's own cursor doesn't move from the same arrows = freeze). Option globals
    // are loaded once at boot by obj_time (room 0, before the title); we still guard reads.
    // We NEVER touch any ini here (the game holds undertale.ini open at the title and GM allows
    // only one open ini) -> changes set nvda_opt_dirty; obj_time flushes them later in the
    // overworld. This is the same option logic as the obj_time (overworld) driver.
    string titleDrive = @"
    if (variable_global_exists(""nvda_menu_open"") && global.nvda_menu_open == 1)
    {
        if (!variable_global_exists(""nvda_ready""))
        {
            global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
            global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
            external_call(global.nvda_init, """");
            global.nvda_ready = 1;
        }
        if (!variable_global_exists(""nvda_menu_wasopen"")) global.nvda_menu_wasopen = 0;
        if (!variable_global_exists(""nvda_menu_sel"")) global.nvda_menu_sel = 0;
        if (!variable_global_exists(""nvda_opt_speech"")) global.nvda_opt_speech = 1;
        if (!variable_global_exists(""nvda_opt_scan""))   global.nvda_opt_scan = 1;
        if (!variable_global_exists(""nvda_opt_skip""))   global.nvda_opt_skip = 1;
        if (!variable_global_exists(""nvda_opt_navsfx"")) global.nvda_opt_navsfx = 1;
        if (!variable_global_exists(""nvda_opt_combat"")) global.nvda_opt_combat = 1;
        if (!variable_global_exists(""nvda_mode""))       global.nvda_mode = 0;
        if (!variable_global_exists(""nvda_musicvol""))   global.nvda_musicvol = 0.5;
        if (!variable_global_exists(""nvda_assist""))     global.nvda_assist = 1;
        if (!variable_global_exists(""nvda_opt_dirty""))  global.nvda_opt_dirty = 0;
        var _announce = 0;
        var _pre = """";
        var _maxsel = 6;
        // close with X (cancel) or K
        if (control_check_pressed(1) || keyboard_check_pressed(ord(""K"")))
        {
            global.nvda_menu_open = 0;
            global.nvda_menu_wasopen = 0;
            external_call(global.nvda_speak, ""Accessibility menu closed."");
            exit;
        }
        // edge: just opened -> intro + first option
        if (global.nvda_menu_wasopen == 0)
        {
            global.nvda_menu_sel = 0;
            _announce = 1;
            _pre = ""Accessibility menu. Up and down to move, left and right to change. Press X or K to close. "";
        }
        if (keyboard_check_pressed(vk_up))   { global.nvda_menu_sel -= 1; _announce = 1; }
        if (keyboard_check_pressed(vk_down)) { global.nvda_menu_sel += 1; _announce = 1; }
        if (global.nvda_menu_sel < 0) global.nvda_menu_sel = _maxsel;
        if (global.nvda_menu_sel > _maxsel) global.nvda_menu_sel = 0;
        var _s = global.nvda_menu_sel;
        var _lr = 0;
        if (keyboard_check_pressed(vk_right)) _lr = 1;
        else if (keyboard_check_pressed(vk_left)) _lr = -1;
        if (_lr != 0)
        {
            if (_s == 0) global.nvda_opt_speech = 1 - global.nvda_opt_speech;
            else if (_s == 1) global.nvda_opt_scan = 1 - global.nvda_opt_scan;
            else if (_s == 2) global.nvda_opt_skip = 1 - global.nvda_opt_skip;
            else if (_s == 3) global.nvda_opt_navsfx = 1 - global.nvda_opt_navsfx;
            else if (_s == 4) global.nvda_opt_combat = 1 - global.nvda_opt_combat;
            else if (_s == 5)
            {
                global.nvda_mode += _lr;
                if (global.nvda_mode < 0) global.nvda_mode = 2;
                if (global.nvda_mode > 2) global.nvda_mode = 0;
                global.nvda_assist = (global.nvda_mode == 0);
            }
            else if (_s == 6)
            {
                global.nvda_musicvol += (_lr * 0.1);
                if (global.nvda_musicvol < 0) global.nvda_musicvol = 0;
                if (global.nvda_musicvol > 1) global.nvda_musicvol = 1;
                if (variable_global_exists(""currentsong"") && is_real(global.currentsong) && global.currentsong > 0)
                    audio_sound_gain(global.currentsong, global.nvda_musicvol, 0);
            }
            _announce = 1;
            global.nvda_opt_dirty = 1;
        }
        if (_announce == 1)
        {
            var _s2 = global.nvda_menu_sel;
            var _lbl = """";
            var _val = """";
            if (_s2 == 0) { _lbl = ""Screen reading""; if (global.nvda_opt_speech == 1) _val = ""on""; else _val = ""off""; }
            else if (_s2 == 1) { _lbl = ""Object scanning and guidance""; if (global.nvda_opt_scan == 1) _val = ""on""; else _val = ""off""; }
            else if (_s2 == 2) { _lbl = ""Puzzle skipping""; if (global.nvda_opt_skip == 1) _val = ""on""; else _val = ""off""; }
            else if (_s2 == 3) { _lbl = ""Navigation sounds""; if (global.nvda_opt_navsfx == 1) _val = ""on""; else _val = ""off""; }
            else if (_s2 == 4) { _lbl = ""Combat cues""; if (global.nvda_opt_combat == 1) _val = ""on""; else _val = ""off""; }
            else if (_s2 == 5)
            {
                _lbl = ""Assist mode"";
                if (global.nvda_mode == 0) _val = ""Assisted, you cannot be defeated"";
                else if (global.nvda_mode == 1) _val = ""Slow, fights run at half speed"";
                else _val = ""Normal, you can be defeated"";
            }
            else if (_s2 == 6) { _lbl = ""Music volume""; _val = string(round(global.nvda_musicvol * 100)) + "" percent""; }
            external_call(global.nvda_speak, _pre + _lbl + "", "" + _val);
        }
        global.nvda_menu_wasopen = 1;
        exit;
    }
";
    gt.QueuePrepend(nm, titleDrive);

    // (b) Save-exists DOWN: Settings(2) -> Accessibility(3) (keep {0,1} -> 2).
    gt.QueueRegexFindReplace(nm,
        @"if \(selected3 == 0 \|\| selected3 == 1\)\s*\{\s*selected3 = 2;\s*\}",
        "if (selected3 == 0 || selected3 == 1)\n{\nselected3 = 2;\n}\nelse if (selected3 == 2)\n{\nselected3 = 3;\n}");

    // (c) Save-exists UP: Accessibility(3) -> Settings(2) (keep Settings(2) -> 0).
    gt.QueueRegexFindReplace(nm,
        @"if \(selected3 == 2\)\s*\{\s*selected3 = 0;\s*\}",
        "if (selected3 == 3)\n{\nselected3 = 2;\n}\nelse if (selected3 == 2)\n{\nselected3 = 0;\n}");

    // (d) No-save DOWN: Settings(1) -> Accessibility(2) (keep Begin(0) -> 1).
    gt.QueueRegexFindReplace(nm,
        @"keyboard_check_pressed\(vk_down\)\)\s*\{\s*if \(selected3 == 0\)\s*\{\s*selected3 = 1;\s*\}\s*\}",
        "keyboard_check_pressed(vk_down))\n{\nif (selected3 == 0)\n{\nselected3 = 1;\n}\nelse if (selected3 == 1)\n{\nselected3 = 2;\n}\n}");

    // (e) No-save UP: Accessibility(2) -> Settings(1) (keep Settings(1) -> 0).
    gt.QueueRegexFindReplace(nm,
        @"keyboard_check_pressed\(vk_up\)\)\s*\{\s*if \(selected3 == 1\)\s*\{\s*selected3 = 0;\s*\}\s*\}",
        "keyboard_check_pressed(vk_up))\n{\nif (selected3 == 1)\n{\nselected3 = 0;\n}\nelse if (selected3 == 2)\n{\nselected3 = 1;\n}\n}");

    gt.Import();
    Console.WriteLine("Injected TITLE-SCREEN accessibility option (4th item in naming==3 menu + freeze guard)");
}

// ===== AUTOWALK (V): grid-pathfind + walk to the selected scanner target =====
// Press V to walk to whatever is currently selected in the object scanner (global.nvda_sel,
// chosen with T/R). Press V again, press any WASD/arrow, leave free-roam, or arrive = stop.
// Uses GameMaker's mp_grid A* to route AROUND walls (obj_solidparent) and hazards (spikes,
// holes) -> if it genuinely can't reach, it says "No path found". Steering is done by setting
// obj_time's up/down/left/right movement flags toward each path node, so the game's own wall
// collision stays as a safety net. Hosted in obj_time Step_1 (Begin Step) AFTER the WASD block
// so it overrides the movement flags for the frame. Gated to overworld free-roam + scan option.
{
    foreach (var f in new string[] { "external_call","variable_global_exists","instance_exists",
             "keyboard_check","keyboard_check_pressed","ord","point_distance","abs","ceil","round",
             "mp_grid_create","mp_grid_destroy","mp_grid_add_rectangle","mp_grid_path","mp_grid_clear_all",
             "mp_grid_clear_rectangle","collision_rectangle",
             "path_add","path_delete","path_get_number","path_get_point_x","path_get_point_y" })
        Data.Functions.EnsureDefined(f, Data.Strings);

    // Follower rewrite (2026-07-08): reliability pass for in-room walking. Steering is now
    // COLLISION-AWARE (predicts a 3px step against obj_solidparent walls and slides along them
    // instead of grinding), the path SELF-HEALS (rebuilds A* from the current spot whenever the
    // body stalls or runs off the end of the path), and a hard "no-gain" timer guarantees it
    // always terminates. mp_grid A* keeps routing around walls/spikes/holes. In-room only.
    string aw = @"
{
    if (!variable_global_exists(""nvda_walk_active""))
    {
        global.nvda_walk_active = 0;
        global.nvda_walk_grid = -1;
        global.nvda_walk_path = -1;
        global.nvda_walk_node = 0;
        global.nvda_walk_target = noone;
        global.nvda_walk_needpath = 0;
        global.nvda_walk_px = 0;
        global.nvda_walk_py = 0;
        global.nvda_walk_noprog = 0;
        global.nvda_walk_bestdist = 99999;
        global.nvda_walk_nogain = 0;
    }
    if (!variable_global_exists(""nvda_ready""))
    {
        global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
        global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
        external_call(global.nvda_init, """");
        global.nvda_ready = 1;
    }

    var _scan_on = (!variable_global_exists(""nvda_opt_scan"") || global.nvda_opt_scan == 1);
    var _aw_ok = (instance_exists(obj_mainchara) && variable_global_exists(""interact"")
                  && global.interact == 0 && !instance_exists(obj_battlecontroller) && _scan_on);
    var _manual = (keyboard_check(ord(""W"")) || keyboard_check(ord(""A"")) || keyboard_check(ord(""S""))
                   || keyboard_check(ord(""D"")) || keyboard_check(vk_up) || keyboard_check(vk_down)
                   || keyboard_check(vk_left) || keyboard_check(vk_right));

    // ---- V pressed: start, or stop if already walking ----
    if (keyboard_check_pressed(ord(""V"")) && _aw_ok)
    {
        if (global.nvda_walk_active == 1)
        {
            if (global.nvda_walk_grid >= 0) mp_grid_destroy(global.nvda_walk_grid);
            if (global.nvda_walk_path >= 0) path_delete(global.nvda_walk_path);
            global.nvda_walk_grid = -1; global.nvda_walk_path = -1;
            global.nvda_walk_active = 0; global.nvda_walk_node = 0;
            up = 0; down = 0; left = 0; right = 0;
            external_call(global.nvda_speak, ""Stopped."");
        }
        else if (!(variable_global_exists(""nvda_sel"") && global.nvda_sel != noone && instance_exists(global.nvda_sel)))
        {
            external_call(global.nvda_speak, ""No target selected. Press T to choose one."");
        }
        else if (point_distance(obj_mainchara.x, obj_mainchara.y, global.nvda_sel.x, global.nvda_sel.y) < 14)
        {
            external_call(global.nvda_speak, ""You are already there."");
        }
        else
        {
            global.nvda_walk_target = global.nvda_sel;
            global.nvda_walk_active = 1;
            global.nvda_walk_needpath = 1;
            global.nvda_walk_node = 0;
            global.nvda_walk_noprog = 0;
            global.nvda_walk_bestdist = 99999;
            global.nvda_walk_nogain = 0;
            global.nvda_walk_px = obj_mainchara.x; global.nvda_walk_py = obj_mainchara.y;
            external_call(global.nvda_speak, ""Walking."");
        }
    }

    // ---- active: (re)build the path as needed, then steer with a collision-aware axis choice ----
    if (global.nvda_walk_active == 1)
    {
        var _tg = global.nvda_walk_target;
        if (!_aw_ok || _tg == noone || !instance_exists(_tg))
        {
            if (global.nvda_walk_grid >= 0) mp_grid_destroy(global.nvda_walk_grid);
            if (global.nvda_walk_path >= 0) path_delete(global.nvda_walk_path);
            global.nvda_walk_grid = -1; global.nvda_walk_path = -1;
            global.nvda_walk_active = 0; global.nvda_walk_node = 0;
            up = 0; down = 0; left = 0; right = 0;
        }
        else if (_manual)
        {
            if (global.nvda_walk_grid >= 0) mp_grid_destroy(global.nvda_walk_grid);
            if (global.nvda_walk_path >= 0) path_delete(global.nvda_walk_path);
            global.nvda_walk_grid = -1; global.nvda_walk_path = -1;
            global.nvda_walk_active = 0; global.nvda_walk_node = 0;
            external_call(global.nvda_speak, ""Stopped."");
        }
        else
        {
            // Work in COLLISION-BOX CENTRE space, not the sprite origin. The origin sits ~24px above
            // the body centre, so an origin-based path demands positions the body can never reach and
            // deadlocks at corners (proven by the walk log). Pathing + following the centre, and
            // inflating walls by the body half-size, makes every planned path actually walkable.
            var _hw = (obj_mainchara.bbox_right - obj_mainchara.bbox_left) / 2;
            var _hh = (obj_mainchara.bbox_bottom - obj_mainchara.bbox_top) / 2;
            var _mcx = (obj_mainchara.bbox_left + obj_mainchara.bbox_right) / 2;
            var _mcy = (obj_mainchara.bbox_top + obj_mainchara.bbox_bottom) / 2;
            var _tgx = (_tg.bbox_left + _tg.bbox_right) / 2;
            var _tgy = (_tg.bbox_top + _tg.bbox_bottom) / 2;
            var _distT = point_distance(_mcx, _mcy, _tgx, _tgy);

            // no-gain backstop: if we haven't got meaningfully closer in ~5s, give up cleanly.
            if (_distT < (global.nvda_walk_bestdist - 1)) { global.nvda_walk_bestdist = _distT; global.nvda_walk_nogain = 0; }
            else global.nvda_walk_nogain += 1;

            if (_distT < (_hw + _hh + 6))
            {
                if (global.nvda_walk_grid >= 0) mp_grid_destroy(global.nvda_walk_grid);
                if (global.nvda_walk_path >= 0) path_delete(global.nvda_walk_path);
                global.nvda_walk_grid = -1; global.nvda_walk_path = -1;
                global.nvda_walk_active = 0; global.nvda_walk_node = 0;
                up = 0; down = 0; left = 0; right = 0;
                external_call(global.nvda_speak, ""Arrived."");
            }
            else if (global.nvda_walk_nogain > 150)
            {
                if (global.nvda_walk_grid >= 0) mp_grid_destroy(global.nvda_walk_grid);
                if (global.nvda_walk_path >= 0) path_delete(global.nvda_walk_path);
                global.nvda_walk_grid = -1; global.nvda_walk_path = -1;
                global.nvda_walk_active = 0; global.nvda_walk_node = 0;
                up = 0; down = 0; left = 0; right = 0;
                if (_distT < (_hw + _hh + 12)) external_call(global.nvda_speak, ""Arrived."");
                else external_call(global.nvda_speak, ""Path blocked. Stopping."");
            }
            else
            {
                // (re)build the grid + A* path FROM THE CURRENT POSITION to the target.
                if (global.nvda_walk_needpath == 1)
                {
                    var _cell = 10;
                    var _cols = ceil(room_width / _cell); var _rows = ceil(room_height / _cell);
                    var _npath = path_add();
                    var _ngrid = mp_grid_create(0, 0, _cols, _rows, _cell, _cell);
                    // Inflate walls by (almost) the body half-size so a CENTRE-point path keeps the
                    // whole body clear of walls. Pass 2 drops the inflation for genuinely tight spots.
                    var _ix = _hw - 2; var _iy = _hh - 2;
                    with (obj_solidparent)     mp_grid_add_rectangle(_ngrid, bbox_left - _ix, bbox_top - _iy, bbox_right + _ix, bbox_bottom + _iy);
                    with (obj_solidnpcparent)  mp_grid_add_rectangle(_ngrid, bbox_left - _ix, bbox_top - _iy, bbox_right + _ix, bbox_bottom + _iy);
                    with (obj_spiketile1)      mp_grid_add_rectangle(_ngrid, bbox_left - _ix, bbox_top - _iy, bbox_right + _ix, bbox_bottom + _iy);
                    with (obj_spiketile2)      mp_grid_add_rectangle(_ngrid, bbox_left - _ix, bbox_top - _iy, bbox_right + _ix, bbox_bottom + _iy);
                    with (obj_spikes_room)     mp_grid_add_rectangle(_ngrid, bbox_left - _ix, bbox_top - _iy, bbox_right + _ix, bbox_bottom + _iy);
                    with (obj_holedown)        mp_grid_add_rectangle(_ngrid, bbox_left - _ix, bbox_top - _iy, bbox_right + _ix, bbox_bottom + _iy);
                    with (obj_superdrophole)   mp_grid_add_rectangle(_ngrid, bbox_left - _ix, bbox_top - _iy, bbox_right + _ix, bbox_bottom + _iy);
                    mp_grid_clear_rectangle(_ngrid, _tg.bbox_left - _hw - 2, _tg.bbox_top - _hh - 2, _tg.bbox_right + _hw + 2, _tg.bbox_bottom + _hh + 2);
                    mp_grid_clear_rectangle(_ngrid, _mcx - _hw - 2, _mcy - _hh - 2, _mcx + _hw + 2, _mcy + _hh + 2);
                    var _found = mp_grid_path(_ngrid, _npath, _mcx, _mcy, _tgx, _tgy, 0);
                    if (!_found)
                    {
                        mp_grid_clear_all(_ngrid);
                        with (obj_solidparent)     mp_grid_add_rectangle(_ngrid, bbox_left, bbox_top, bbox_right, bbox_bottom);
                        with (obj_solidnpcparent)  mp_grid_add_rectangle(_ngrid, bbox_left, bbox_top, bbox_right, bbox_bottom);
                        with (obj_spiketile1)      mp_grid_add_rectangle(_ngrid, bbox_left, bbox_top, bbox_right, bbox_bottom);
                        with (obj_spiketile2)      mp_grid_add_rectangle(_ngrid, bbox_left, bbox_top, bbox_right, bbox_bottom);
                        with (obj_spikes_room)     mp_grid_add_rectangle(_ngrid, bbox_left, bbox_top, bbox_right, bbox_bottom);
                        with (obj_holedown)        mp_grid_add_rectangle(_ngrid, bbox_left, bbox_top, bbox_right, bbox_bottom);
                        with (obj_superdrophole)   mp_grid_add_rectangle(_ngrid, bbox_left, bbox_top, bbox_right, bbox_bottom);
                        mp_grid_clear_rectangle(_ngrid, _tg.bbox_left - _hw - 2, _tg.bbox_top - _hh - 2, _tg.bbox_right + _hw + 2, _tg.bbox_bottom + _hh + 2);
                        mp_grid_clear_rectangle(_ngrid, _mcx - _hw - 2, _mcy - _hh - 2, _mcx + _hw + 2, _mcy + _hh + 2);
                        _found = mp_grid_path(_ngrid, _npath, _mcx, _mcy, _tgx, _tgy, 0);
                    }
                    if (_found)
                    {
                        if (global.nvda_walk_grid >= 0) mp_grid_destroy(global.nvda_walk_grid);
                        if (global.nvda_walk_path >= 0) path_delete(global.nvda_walk_path);
                        global.nvda_walk_grid = _ngrid;
                        global.nvda_walk_path = _npath;
                        global.nvda_walk_node = 0;
                        global.nvda_walk_needpath = 0;
                    }
                    else
                    {
                        mp_grid_destroy(_ngrid);
                        path_delete(_npath);
                        if (global.nvda_walk_path < 0)
                        {
                            // never had any path = unreachable from the very start.
                            global.nvda_walk_active = 0; global.nvda_walk_node = 0;
                            up = 0; down = 0; left = 0; right = 0;
                            external_call(global.nvda_speak, ""No path found."");
                        }
                        else
                        {
                            // keep following the last good path; the no-gain timer is the backstop.
                            global.nvda_walk_needpath = 0;
                        }
                    }
                }

                // steer along the current path
                if (global.nvda_walk_active == 1 && global.nvda_walk_path >= 0)
                {
                    var _p = global.nvda_walk_path;
                    var _total = path_get_number(_p);
                    var _n = global.nvda_walk_node;
                    var _guard = 0;
                    while (_n < _total && _guard < 400 && point_distance(_mcx, _mcy, path_get_point_x(_p, _n), path_get_point_y(_p, _n)) < 8)
                    {
                        _n += 1; _guard += 1;
                    }
                    global.nvda_walk_node = _n;
                    if (_n >= _total)
                    {
                        // ran out of path but not at the target -> reroute next frame.
                        global.nvda_walk_needpath = 1;
                        up = 0; down = 0; left = 0; right = 0;
                    }
                    else
                    {
                        var _nx = path_get_point_x(_p, _n);
                        var _ny = path_get_point_y(_p, _n);
                        var _ddx = _nx - _mcx;
                        var _ddy = _ny - _mcy;
                        var _hx = 0; var _vy = 0;
                        if (_ddx > 2) _hx = 3; else if (_ddx < -2) _hx = -3;
                        if (_ddy > 2) _vy = 3; else if (_ddy < -2) _vy = -3;
                        // Predict each candidate step against walls. obj_solidparent reverts BOTH axes on
                        // contact, so: only command an axis whose 3px step is clear, and only command
                        // BOTH (a diagonal) when the diagonal destination is also clear. This heads
                        // straight at the waypoint and SLIDES along a wall when one axis is blocked,
                        // instead of grinding to a halt or veering off the route.
                        var _bl = obj_mainchara.bbox_left; var _bt = obj_mainchara.bbox_top;
                        var _br = obj_mainchara.bbox_right; var _bb = obj_mainchara.bbox_bottom;
                        // A step is clear only if it hits NEITHER walls (obj_solidparent) NOR solid
                        // interactables (obj_solidnpcparent: desks/signs/NPCs/save points) - the game
                        // blocks on both (obj_mainchara Collision_1367 + _822), so the follower must too.
                        var _canh = (_hx != 0
                            && collision_rectangle(_bl + _hx, _bt, _br + _hx, _bb, obj_solidparent, false, true) == noone
                            && collision_rectangle(_bl + _hx, _bt, _br + _hx, _bb, obj_solidnpcparent, false, true) == noone);
                        var _canv = (_vy != 0
                            && collision_rectangle(_bl, _bt + _vy, _br, _bb + _vy, obj_solidparent, false, true) == noone
                            && collision_rectangle(_bl, _bt + _vy, _br, _bb + _vy, obj_solidnpcparent, false, true) == noone);
                        var _cand = (_hx != 0 && _vy != 0
                            && collision_rectangle(_bl + _hx, _bt + _vy, _br + _hx, _bb + _vy, obj_solidparent, false, true) == noone
                            && collision_rectangle(_bl + _hx, _bt + _vy, _br + _hx, _bb + _vy, obj_solidnpcparent, false, true) == noone);
                        up = 0; down = 0; left = 0; right = 0;
                        var _go = 0;   // 0 = fully blocked; 1 = moved at least one axis this frame
                        if (_canh && _canv && _cand)
                        {
                            if (_hx > 0) right = 1; else left = 1;
                            if (_vy > 0) down = 1; else up = 1;
                            _go = 1;
                        }
                        else if (abs(_ddx) >= abs(_ddy))
                        {
                            if (_canh) { if (_hx > 0) right = 1; else left = 1; _go = 1; }
                            else if (_canv) { if (_vy > 0) down = 1; else up = 1; _go = 1; }
                        }
                        else
                        {
                            if (_canv) { if (_vy > 0) down = 1; else up = 1; _go = 1; }
                            else if (_canh) { if (_hx > 0) right = 1; else left = 1; _go = 1; }
                        }

                        // stall detection -> reroute (or, if we're right next to a solid target, arrive).
                        if (abs(_mcx - global.nvda_walk_px) < 1 && abs(_mcy - global.nvda_walk_py) < 1)
                            global.nvda_walk_noprog += 1;
                        else
                            global.nvda_walk_noprog = 0;
                        global.nvda_walk_px = _mcx; global.nvda_walk_py = _mcy;
                        if (_go == 0 || global.nvda_walk_noprog > 8)
                        {
                            if (_distT < (_hw + _hh + 10))
                            {
                                if (global.nvda_walk_grid >= 0) mp_grid_destroy(global.nvda_walk_grid);
                                if (global.nvda_walk_path >= 0) path_delete(global.nvda_walk_path);
                                global.nvda_walk_grid = -1; global.nvda_walk_path = -1;
                                global.nvda_walk_active = 0; global.nvda_walk_node = 0;
                                up = 0; down = 0; left = 0; right = 0;
                                external_call(global.nvda_speak, ""Arrived."");
                            }
                            else
                            {
                                global.nvda_walk_needpath = 1;
                                global.nvda_walk_noprog = 0;
                            }
                        }
                    }
                }
            }
        }
    }
}";
    var awg = new UndertaleModLib.Compiler.CodeImportGroup(Data);
    awg.QueueAppend(Data.Code.ByName("gml_Object_obj_time_Step_1"), aw);
    awg.Import();
    Console.WriteLine("Injected AUTOWALK (V key; mp_grid pathfinding to selected scanner target)");
}

// ===== BLUE SOUL (jump) cue v2 — obj_heart movement==2: type-aware sonar + AUTO JUMP HEIGHT =====
// Blue soul = heart stands on the floor, left/right walk, UP = jump. The game jump is VARIABLE
// height (hold UP = higher). Papyrus throws SHORT floor bones (little hop), TALL floor bones (must
// hold for a big jump) and CEILING bones (blt_topbone: stay LOW). v1 gave one "jump" beep for all
// -> couldn't tell short vs tall vs "don't jump". v2 (Lilian's call: "auto-height + full sounds"):
//   * TYPE-AWARE CUE while grounded: nearest floor bone -> panned beep, HIGH pitch = short (tap),
//     LOW pitch = tall (big jump); ceiling-only -> a steady low "stay down" pulse. Rising pitch +
//     faster rate as it nears = act-now (same feel as the FIGHT/green-shield cues she liked).
//   * AUTO JUMP HEIGHT: she just presses UP on the beat; the mod holds the jump exactly high enough
//     to clear the tallest floor bone in her column (clamped by ceiling bones + the box top), then
//     drops her -> lands in time for the next. Done by overriding vspeed at the END of Step_0 (after
//     the game's jump-cut + gravity), so it works regardless of global.osflavor / whether she holds.
//   * LANDING TICK when she becomes grounded again = "you can jump the next one" (rhythm of a run).
// Gated movement==2 + combat-cues option. Death still prevented by Assist (M) mode while learning.
// Rotated-gravity variants (movement 11/12/13) + Sans' obj_heart_sansbattle still deferred.
{
    foreach (var f in new string[] { "external_define","external_call","variable_global_exists",
             "variable_instance_exists","abs","round" })
        Data.Functions.EnsureDefined(f, Data.Strings);

    string bs = @"
{
    if (movement == 2 && global.mnfight == 2
        && (!variable_global_exists(""nvda_opt_combat"") || global.nvda_opt_combat == 1))
    {
        if (!variable_global_exists(""pan_ready""))
        {
            global.pan_init = external_define(""gmpan.dll"", ""gmpan_init"", 0, 0, 0);
            global.pan_beep = external_define(""gmpan.dll"", ""gmpan_beep"", 0, 0, 4, 0, 0, 0, 0);
            external_call(global.pan_init);
            global.pan_ready = 1;
        }
        if (!variable_global_exists(""nvda_ready""))
        {
            global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
            global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
            external_call(global.nvda_init, """");
            global.nvda_ready = 1;
        }
        if (!variable_instance_exists(id, ""nvda_bs_lastjs"")) nvda_bs_lastjs = jumpstage;
        if (!variable_instance_exists(id, ""nvda_bs_climb"")) nvda_bs_climb = 0;
        if (!variable_instance_exists(id, ""nvda_bs_greeted"")) nvda_bs_greeted = 0;
        if (!variable_global_exists(""nvda_jumptimer"")) global.nvda_jumptimer = 0;

        if (nvda_bs_greeted == 0)
        {
            external_call(global.nvda_speak, ""Blue soul. Press up to jump. The mod sets your jump height, so just time each jump to the beeps. A high beep is a small bone, a low beep is a big bone, and a slow low pulse means a ceiling bone, so stay down."");
            nvda_bs_greeted = 1;
        }

        var _floory = global.idealborder[3] - 16;    // grounded-heart line
        var _boxtop = global.idealborder[2] + 6;      // highest she can be

        // ---- scan blue-soul bones (blt_sizebone + its children blt_superbone/blt_topbone) ----
        var _fgap = 9999; var _fdx = 0; var _ftop = _floory; var _ffound = 0;  // nearest FLOOR bone
        var _cgap = 9999; var _cdx = 0; var _cbot = _boxtop; var _cfound = 0;  // nearest CEILING bone
        var _clearTop = _floory;                       // tallest floor-bone top in the takeoff column
        with (blt_sizebone)
        {
            var _g = abs(x - other.x);
            if (_g < 130)
            {
                if (object_index == blt_topbone)
                {
                    if (_g < _cgap) { _cgap = _g; _cdx = x - other.x; _cbot = bbox_bottom; _cfound = 1; }
                }
                else
                {
                    if (_g < _fgap) { _fgap = _g; _fdx = x - other.x; _ftop = bbox_top; _ffound = 1; }
                    if (_g < 70 && bbox_top < _clearTop) _clearTop = bbox_top;
                }
            }
        }

        var _grounded = (jumpstage != 2);

        // ================= TYPE-AWARE CUE (grounded only) =================
        if (_grounded)
        {
            if (global.nvda_jumptimer > 0) global.nvda_jumptimer -= 1;
            if (global.nvda_jumptimer <= 0)
            {
                if (_ffound == 1)
                {
                    var _close = 1 - (_fgap / 130); if (_close < 0) _close = 0; if (_close > 1) _close = 1;
                    var _pan = _fdx / 130; if (_pan < -1) _pan = -1; if (_pan > 1) _pan = 1;
                    var _freq = 520 + (_close * 320); var _ms = 50; var _gain = 0.6;   // short = HIGH
                    if ((_floory - _ftop) > 70) { _freq = 260 + (_close * 220); _ms = 85; _gain = 0.68; } // tall = LOW
                    external_call(global.pan_beep, _pan, _freq, _ms, _gain);
                    var _iv = round(16 - (_close * 13)); if (_iv < 3) _iv = 3;
                    global.nvda_jumptimer = _iv;
                }
                else if (_cfound == 1)
                {
                    var _panc = _cdx / 130; if (_panc < -1) _panc = -1; if (_panc > 1) _panc = 1;
                    external_call(global.pan_beep, _panc, 200, 60, 0.5);   // steady low = stay down
                    global.nvda_jumptimer = 10;
                }
            }
        }

        // ================= AUTO JUMP HEIGHT (airborne) =================
        if (jumpstage == 2 && nvda_bs_lastjs != 2) nvda_bs_climb = 1;   // takeoff this frame
        if (jumpstage != 2) nvda_bs_climb = 0;
        if (nvda_bs_climb == 1 && jumpstage == 2)
        {
            var _yt = _clearTop - 10;                          // rise just above the tallest bone
            if (_ffound == 0 && _clearTop >= _floory) _yt = _floory - 26;   // no bone -> small hop
            if (_yt < _boxtop) _yt = _boxtop;                  // never leave the box top
            if (_cfound == 1 && _yt < (_cbot + 10)) _yt = _cbot + 10;   // don't rise into a ceiling bone
            if (y > _yt)
            {
                if (vspeed > -6) vspeed = -6;                  // keep climbing (beats the jump-cut)
            }
            else
            {
                nvda_bs_climb = 0;
                if (vspeed < 0) vspeed = 0;                    // apex reached -> let gravity drop her
            }
        }

        // ================= LANDING TICK =================
        if (jumpstage != 2 && nvda_bs_lastjs == 2)
            external_call(global.pan_beep, 0, 380, 35, 0.35);
        nvda_bs_lastjs = jumpstage;
    }
    else
    {
        if (variable_instance_exists(id, ""nvda_bs_greeted"")) nvda_bs_greeted = 0;
    }
}";
    var bsg = new UndertaleModLib.Compiler.CodeImportGroup(Data);
    bsg.QueueAppend(Data.Code.ByName("gml_Object_obj_heart_Step_0"), bs);
    bsg.Import();
    Console.WriteLine("Injected BLUE-SOUL jump cue v2 (auto-height + type-aware sonar on obj_heart)");
}
