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
        if (_c == ""&"" || _c == ""#"") { _out += "" ""; _i += 1; continue; }
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
        if (_ck == ""flowey"") _desc = ""Una pequeña flor dorada asoma por el suelo. Tiene una cara redonda y blanca, dos grandes ojos oscuros y una amplia sonrisa alegre."";
        else if (_ck == ""toriel"") _desc = ""Un monstruo alto y amable que parece una cabra erguida sobre dos patas. Tiene un pelaje blanco y suave, largas orejas caídas, cuernos pequeños y ojos cálidos, y viste una larga túnica morada con un escudo alado blanco."";
        else if (_ck == ""sans"") _desc = ""Un esqueleto bajito y rechoncho con una amplia sonrisa permanente. Lleva una chaqueta azul con capucha sobre una camiseta blanca, pantalones cortos negros y zapatillas rosas, y parece completamente relajado."";
        else if (_ck == ""papyrus"") _desc = ""Un esqueleto muy alto y larguirucho que adopta una pose dramática. Lleva un disfraz casero: un peto blanco con una larga bufanda roja, guantes rojos y botas rojas."";
        else if (_ck == ""undyne"") _desc = ""Un monstruo alto y poderoso con aspecto de pez, de escamas azul oscuro, una larga coleta roja y afilados dientes amarillos. Un ojo lo lleva cubierto con un parche negro, y viste una reluciente armadura de metal."";
        else if (_ck == ""alphys"") _desc = ""Un monstruo bajito y regordete de color amarillo, con aspecto de lagarto, gafas y una expresión algo nerviosa. Viste una bata blanca de laboratorio."";
        else if (_ck == ""asgore"") _desc = ""Un monstruo enorme y poderoso, como una cabra erguida, con la corpulencia de un rey de anchos hombros. Tiene pelaje blanco, largos cuernos curvos, melena y barba doradas, y viste una armadura morada."";
        else if (_ck == ""asriel"") _desc = ""Un joven monstruo con aspecto de cabra, un niño de pelaje blanco y suave, largas orejas caídas y cuernos pequeños. Viste una túnica verde con una única franja amarilla en el centro."";
        else if (_ck == ""mettaton"") _desc = ""Un robot con forma de caja metálica rectangular que se sostiene sobre una única rueda. Su parte frontal está cubierta de diales, botones y una pequeña pantalla, y de sus costados salen dos brazos delgados con guantes blancos."";
        else if (_ck == ""temmie"") _desc = ""Una pequeña y extraña criatura dibujada con un estilo tosco y garabateado: una cara felina de grandes orejas y ojos muy abiertos sobre un pequeño cuerpo peludo de color marrón."";
        else if (_ck == ""gaster"") _desc = ""Una figura alta y sombría que parece derretirse por los bordes. Su rostro pálido y agrietado tiene una grieta que sube desde un ojo y otra que baja, y habla mediante símbolos extraños."";
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
    "La alegre cara de Flowey se retuerce de pronto en algo cruel. Los perdigones se vuelven contra ti, formando un anillo alrededor de tu alma que se cierra para rodearte.");
beats += ObjBeat("toriel_rescue", "obj_torielcutscene",
    "Una bola de fuego llega surcando el aire y golpea a la flor, quitándola de en medio de un golpe. Un monstruo alto y maternal sale de las sombras hacia ti.");
beats += ObjBeat("toriel_fight", "obj_torielboss",
    "Toriel permanece bloqueando la gran puerta de piedra, y las llamas florecen en sus manos. Sin embargo, cada vez que ataca, su fuego se desvía de ti en el último momento. No es capaz de hacerte daño de verdad.");
// The spare goodbye is done by the OVERWORLD actor obj_toroverworld3 in room_basement4 (NOT the battle
// object obj_torielboss, which dies during the battle->overworld fade). It shows spr_toriel_hug at the
// embrace. Fire on the rising edge of the hug sprite, checked INSIDE an instance_exists guard (separate
// nested ifs, so we never read .sprite_index on a missing instance). Re-arms when not hugging.
beats += "    if (room == room_basement4 && instance_exists(obj_toroverworld3)) {\n" +
    "        var _hug = (obj_toroverworld3.sprite_index == spr_toriel_hug || obj_toroverworld3.sprite_index == spr_toriel_hug2 || obj_toroverworld3.sprite_index == spr_toriel_hug3);\n" +
    "        if (!variable_global_exists(\"nvda_b_torgood\")) global.nvda_b_torgood = 0;\n" +
    "        if (_hug && global.nvda_b_torgood == 0) { global.nvda_b_torgood = 1;" +
    CsSpeak("Toriel te envuelve en un cálido abrazo, estrechándote contra ella durante un largo instante. Luego te suelta, se da la vuelta y se aleja por el pasillo, dejándote continuar solo.") +
    " } else if (_hug == 0) global.nvda_b_torgood = 0;\n" +
    "    }\n";
// ghost-2 fades out where he sits after his dialogue (his Step fades image_alpha to 0 + self-destroy).
beats += FallBeat("ghost2_leaves", "obj_napstablook2",
    "El pequeño fantasma se desvanece lentamente, volviéndose cada vez más tenue, hasta que desaparece de la vista.");
// obj_darksanstrigger is placed in the room (exists from entry); the approaching Sans actor
// obj_darksans1 is created only on COLLISION (when you walk into the trigger) = the real moment.
beats += ObjBeat("sans_meet", "obj_darksans1",
    "El camino de delante está oscuro y silencioso. A tu espalda, unos pasos suaves se acercan, y una figura baja y sombría se aproxima y te tiende una mano.");
// ===== UNDYNE ARC (Waterfall + Hotland). She is SILENT in these scenes, so dialogue narration gives
//       a blind player nothing - this is pure visual action. Keyed on room + the actual actor object
//       (obj_undynea_actor / _actor2 / obj_undynefall), which only exist while the scene is playing,
//       so no misfire on backtrack (the room-placed trigger persists; the actor does not). =====
// Timed on each encounter's internal counter going nonzero (= you walked PAST the trigger and the
// scene fires), NOT the actor merely existing (it is created invisible at room load). Encounter 2
// sets cn=1 as pre-setup, so its real start is cn >= 2 (control locks, Undyne begins the grab).
beats += ActorStateBeat("undyne_ledge", "obj_undyneencounter1", "obj_undyneencounter1.cn > 0",
    "Una lanza de luz azul se clava en el suelo junto a ti. En lo alto del saliente de arriba se yergue una alta figura con armadura, fulminándote con la mirada a través de un yelmo con cuernos.");
beats += ActorStateBeat("undyne_grass", "obj_undyneencounter2", "obj_undyneencounter2.cn >= 2",
    "Te agachas escondido entre la hierba alta. Unos pesados pasos con armadura se acercan y se detienen justo a tu lado. Una mano enguantada desciende, se cierra en torno al pequeño monstruo infantil que tienes al lado, lo levanta por la cabeza y luego lo deja en el suelo y se marcha a grandes zancadas.");
beats += ActorStateBeat("undyne_bridge", "obj_undyneencounter3", "obj_undyneencounter3.cn > 0",
    "La guerrera con armadura cae sobre el puente a tu espalda y te persigue, arrojando lanza tras lanza de luz azul mientras corres.");
beats += ActorStateBeat("undyne_fall", "obj_undyneencounter4", "obj_undyneencounter4.con > 0",
    "Acorralado al final del puente, solo puedes retroceder mientras ella avanza, hasta que la pasarela cede bajo tus pies y te precipitas hacia la oscuridad.");
beats += ObjBeat("undyne_collapse", "obj_undynefall",
    "Vencida por el calor sofocante dentro de su pesada armadura, la guerrera se tambalea, trastabilla y por fin se desploma contra el suelo, donde queda inmóvil.");

// ===== BACK-HALF STORY CUTSCENES (Hotland -> CORE -> New Home -> neutral finale). Silent visual
//       action a blind player would otherwise miss, authored from the cutscene log of Lilian's
//       neutral playthrough (2026-07-08). Each keyed on the beat-MOMENT scene object (per the
//       "the actor existing != the scene happening" rule) and room-gated for safety. =====

// Mettaton's box form transforms into his glamorous EX body (obj_scrollaway_event = the dramatic
// scroll-up reveal spawned at the transformation; his box form was already covered on first meeting).
beats += EdgeBeat("mett_ex", "room == room_fire_core_metttest && instance_exists(obj_scrollaway_event)",
    "Entre un estallido de luz y confeti, la verdadera forma del robot con forma de caja se despliega: una máquina alta y glamurosa de piernas largas y esbeltas, un brazo extendido en una pose, el pelo oscuro cayéndole sobre un ojo y un corazón brillante en el pecho. Mettaton EX ha llegado, y es fabuloso.");

// Cooking show: Mettaton launches his whole counter into the sky and you chase it up on a jetpack.
// NOTE: this is also a minigame Lilian failed -> see the gameplay-guidance track (memory) for later.
beats += EdgeBeat("cook_jetpack", "room == room_fire_cookingshow && instance_exists(obj_phonetojetpack)",
    "Toda la encimera de cocina sale disparada del escenario y se eleva hacia el cielo, con Mettaton subido en ella. Suena un teléfono, una mochila propulsora se ata a tu espalda y sales disparado tras él, surcando el aire.");

// Throne room: the King watering the golden flowers, back turned, before he notices you (con>=2 =
// the scene has triggered; he turns to face you later at con 19+, which his first line covers).
beats += ActorStateBeat("asgore_flowers", "obj_asgoremeet_event", "obj_asgoremeet_event.con >= 2",
    "Al fondo de la sala, una figura enorme y corpulenta con ropas reales está de espaldas a ti, con sus grandes cuernos curvos inclinados mientras riega con delicadeza el lecho de flores doradas, tarareando para sí mismo, aún sin advertir que alguien ha llegado.");

// Asgore's fight begins: he destroys the MERCY button so you cannot spare him (obj_asgoreb Create
// sets obj_sparebt.visible = false = the mechanical realisation of the smash).
beats += EdgeBeat("asgore_nomercy", "room == room_castle_barrier && instance_exists(obj_asgoreb)",
    "El Rey alza su gran tridente y lo descarga sobre el botón de PIEDAD, haciéndolo añicos. Ya no hay forma de dialogar, ni de perdonarlo: solo queda luchar.");

// The six human souls rise into the air at the barrier (barrierevent spawns obj_heartcontainer).
beats += EdgeBeat("barrier_souls", "room == room_castle_barrier && instance_exists(obj_heartcontainer)",
    "Seis corazones invertidos ascienden flotando en la oscuridad y quedan suspendidos en el aire a tu alrededor, cada uno de un color distinto: cian, naranja, azul, morado, verde y dorado. Las seis almas humanas que el Rey ha reunido, todo lo necesario para al fin romper la barrera.");

// Finale (room_f_room): the flower shatters your SAVE file (obj_savepoint_fake.crack counts up as
// each blow lands, then the file is erased and Flowey's grinning face rises).
beats += ActorStateBeat("finale_erase", "obj_savepoint_fake", "obj_savepoint_fake.crack >= 1",
    "Toda la pantalla se sacude y se resquebraja. Tres golpes atronadores la parten como el cristal, y tu propio archivo de GUARDADO se hace pedazos y se borra hasta no quedar nada. De la oscuridad surge una pequeña flor dorada, con una amplia y burlona sonrisa.");

// Finale: Photoshop / Omega Flowey reveal (obj_floweybattler2 = the boss).
beats += EdgeBeat("finale_photoshop", "room == room_f_room && instance_exists(obj_floweybattler2)",
    "La flor se ha fusionado con tu GUARDADO robado y las seis almas para convertirse en algo monstruoso: una mole colosal de acero, cables y pantallas de televisión, con maquinaria que zumba y destella alrededor de un rostro enorme y deforme que se asoma desde su mismo centro. Se cierne sobre ti, inmenso y sonriente.");

// Finale aftermath: the six souls turn on Flowey and protect you (obj_6soul_helpcutscene).
beats += ObjBeat("finale_souls_help", "obj_6soul_helpcutscene",
    "Una a una, las seis almas humanas vuelven a cobrar vida dentro de la máquina y se rebelan contra la flor, rodeándote, protegiéndote y curando tus heridas mientras se alzan contra su captor.");

// ===== ROOM AMBIANCE: environmental descriptions. Spoken ONCE per save on first visit; the L key
//       re-speaks the current room any time (AmbBeat records global.nvda_roomdesc). All verified
//       against _rooms_all.txt. =====
// ---- Ruins ----
beats += AmbBeat("amb_area1", "room == room_area1 || room == room_area1_2",
    "Aterrizas ileso en un lecho de flores doradas, en las profundidades del subsuelo. Una luz pálida se filtra desde un agujero en lo alto.");
beats += AmbBeat("amb_ruins2", "room == room_ruins2",
    "Una pequeña cámara de piedra. Hay una placa de presión encastrada en el suelo y una palanca que sobresale de la pared. Accionar ambas abre la puerta de delante.");
beats += AmbBeat("amb_ruins3", "room == room_ruins3",
    "Una sala cruzada por un lecho de pinchos. Hileras de interruptores recorren las paredes, y Toriel ha marcado con flechas los que necesitas, para que puedas bajar los pinchos y cruzar sin peligro.");
beats += AmbBeat("amb_ruins4", "room == room_ruins4",
    "Un muñeco de entrenamiento de tela se alza sobre un soporte de madera en el centro de la sala, con su cara cosida inexpresiva y paciente.");
beats += AmbBeat("amb_ruins5", "room == room_ruins5",
    "Un largo pasillo. Zonas de pinchos salpican el suelo, con un camino seguro que serpentea entre ellas.");
beats += AmbBeat("amb_ruins6", "room == room_ruins6",
    "Un pasillo largo y recto se extiende hacia la penumbra, con el extremo perdido en la sombra. El aire está quieto, y tus propios pasos silenciosos son el único sonido.");
beats += AmbBeat("amb_ruins7a", "room == room_ruins7A",
    "Aquí, sobre un pedestal, descansa un cuenco de caramelos de colores, junto a un pequeño cartel.");
beats += AmbBeat("amb_ruins9", "room == room_ruins9",
    "Una roca reposa en el suelo cerca de un tramo de pinchos. Empújala sobre el interruptor para mantener los pinchos bajados y cruza mientras están hundidos.");
beats += AmbBeat("amb_ruins10", "room == room_ruins10",
    "Una sala tranquila con agujeros desgastados en el suelo y dos placas de piedra encastradas en las paredes, con sus viejas inscripciones esperando a ser leídas.");
beats += AmbBeat("amb_ruins11", "room == room_ruins11",
    "Una gran roca gris reposa en el suelo, y encastrado en el suelo cerca hay un interruptor, una placa de presión. Para mover la roca, empújala caminando desde el lado opuesto y se deslizará un espacio cada vez. Empújala sobre el interruptor para abrir el camino.");
beats += AmbBeat("amb_ruins12", "room == room_ruins12",
    "Un pequeño fantasma blanco yace tendido sobre el camino de delante, tenue y semitransparente. Las letras z, z, z flotan sobre él mientras finge estar dormido, aunque no parece estar engañando a nadie.");
beats += AmbBeat("amb_ruins12a", "room == room_ruins12A",
    "Una cuña de queso reposa sobre una mesa en el centro de la sala, pegada firmemente tras haber quedado intacta durante años. En la pared del fondo, una diminuta ratonera espera en un silencio esperanzado.");
beats += AmbBeat("amb_ruins14", "room == room_ruins14",
    "Una sala alta. Justo delante, un agujero en el suelo desciende a un nivel inferior. Abajo hay un interruptor encastrado en el suelo, un par de rechonchos monstruos vegetales medio enterrados en la tierra, una cinta descolorida tirada en el suelo y un pequeño y afligido fantasma que descansa en silencio en uno de los huecos.");
beats += AmbBeat("amb_ruins15", "room == room_ruins15A",
    "Una sala tenue salpicada de altos pilares. Entre ellos hay interruptores de colores (uno rojo, uno verde y uno azul), y unos pinchos bloquean el paso hasta que se pulsan los correctos.");
beats += AmbBeat("amb_ruins18", "room == room_ruins18OLD",
    "Aquí, en el suelo, yace un pequeño cuchillo de juguete, esperando a que lo recojan.");
// ---- Toriel's home ----
beats += AmbBeat("amb_home_yard", "room == room_ruins19",
    "Un gran árbol negro sin hojas se alza en un pequeño patio, con sus ramas desnudas muy extendidas y hojas secas esparcidas por todas partes. Justo al otro lado, un acogedor hogar está construido en la roca, con una cálida luz derramándose por sus ventanas.");
beats += AmbBeat("amb_home_entrance", "room == room_torhouse1",
    "Entras en el hogar de Toriel, cálido y acogedor tras la fría piedra de las Ruinas. Una escalera baja a tu izquierda, y un pasillo se abre a tu derecha.");
beats += AmbBeat("amb_home_living", "room == room_torhouse2",
    "Un acogedor salón. Un fuego crepita en el hogar, y junto a él hay un gran sillón de lectura acolchado con un libro abierto sobre el brazo. Una puerta da paso a una pequeña cocina.");
beats += AmbBeat("amb_home_hallway", "room == room_torhouse3",
    "Un largo pasillo flanqueado de puertas, con una luz suave. Un espejo cuelga de una pared y una pequeña lámpara brilla en un rincón. Una de las puertas se ha preparado como dormitorio, solo para ti.");
// ---- Rooms Lilian flagged as missing on her Ruins walk (all verified against the room dump) ----
beats += AmbBeat("amb_ruins1", "room == room_ruins1",
    "Un alto vestíbulo de entrada de piedra morada oscura, donde una amplia escalera sube y se adentra en las Ruinas. Un halo de luz suave marca aquí un punto de guardado.");
// The leaf pile: the room immediately after the "unnecessary tension" corridor (candy bowl above,
// first rock puzzle to the right) = room_ruins7, which also holds a frog and a save point.
beats += AmbBeat("amb_ruins7", "room == room_ruins7",
    "Una sala alfombrada por una espesa capa de hojas rojas y secas que crujen suavemente bajo tus pies. Un pequeño monstruo con aspecto de rana descansa junto a la pared, y cerca brilla un punto de guardado. De aquí salen pasajes, uno hacia arriba y otro hacia la derecha.");
beats += AmbBeat("amb_ruins8", "room == room_ruins8",
    "Una sala alta y estrecha con un agujero en el suelo por el que puedes dejarte caer al nivel inferior.");
beats += AmbBeat("amb_ruins13", "room == room_ruins13",
    "Una sala donde un par de pequeños monstruos con aspecto de rana descansan junto a las paredes. Con gusto te darán consejos, y un cartel cercano explica cómo funcionan perdonar a un monstruo, hacer una pausa y saltar el texto.");
beats += AmbBeat("amb_ruins16", "room == room_ruins16",
    "Una sala de piedra más amplia donde confluyen varios pasajes.");
beats += AmbBeat("amb_ruins17", "room == room_ruins17",
    "Un pequeño monstruo con aspecto de rana está aquí sentado en silencio, dispuesto a ofrecerte un consejo si le hablas.");
beats += AmbBeat("amb_ruins12b", "room == room_ruins12B",
    "Dos telarañas se extienden por un rincón de esta sala, una pequeña y una grande, con un pequeño cartel al lado. Es una venta benéfica de arañas: deja unas monedas en una telaraña y las arañas te venderán un dulce.");
// ---- Toriel's home: the two bedrooms off the hallway ----
beats += AmbBeat("amb_home_bedroom", "room == room_asrielroom",
    "El acogedor dormitorio de un niño. Una cama bien hecha reposa bajo una lámpara suave, con una caja de juguetes y algunas pequeñas comodidades repartidas por la habitación. Se ha preparado solo para ti.");
beats += AmbBeat("amb_home_torielroom", "room == room_torielroom",
    "El propio dormitorio de Toriel, ordenado y cálido. Una pequeña silla ocupa un rincón, y hay algunas de sus cosas personales que puedes leer si echas un vistazo.");
// ---- The basement corridor Toriel leads you down at the end of the Ruins ----
beats += AmbBeat("amb_basement1", "room == room_basement1",
    "Un largo y frío pasillo de sótano de piedra gris, que se aleja de la calidez de la casa de arriba.");
beats += AmbBeat("amb_basement2", "room == room_basement2",
    "El frío pasillo de piedra continúa, silencioso y en penumbra.");
beats += AmbBeat("amb_basement3", "room == room_basement3",
    "El pasaje se estrecha, y el aire se vuelve más frío cuanto más avanzas.");
beats += AmbBeat("amb_basement4", "room == room_basement4",
    "Un breve tramo de pasillo, con la piedra desgastada y lisa bajo tus pies.");
beats += AmbBeat("amb_basement5", "room == room_basement5",
    "Un pasaje muy largo y recto que se extiende hacia el frío en la distancia. En su lejano extremo se alza una gran puerta que conduce fuera de las Ruinas.");
// ---- Snowdin: the snowy forest (all verified against _rooms_all.txt, room_tundra*) ----
beats += AmbBeat("amb_tundra1", "room == room_tundra1",
    "La gran puerta de piedra de las Ruinas se cierra a tu espalda, y ante ti el camino se abre a un bosque silencioso y cubierto de nieve. Árboles desnudos se apiñan a ambos lados, y tu aliento se empaña en el aire frío y quieto.");
beats += AmbBeat("amb_tundra2", "room == room_tundra2",
    "Un largo camino que serpentea entre bosques nevados. Una gran rama yace caída atravesando el paso, y más adelante se alza una extraña verja de madera, con los barrotes tan separados que cualquiera podría pasar sencillamente entre ellos. A un lado hay una lámpara de forma convenientemente oportuna.");
beats += AmbBeat("amb_tundra3", "room == room_tundra3",
    "Un claro en el bosque. Una garita de vigilancia de madera, poco más que un puesto de guardia de barrotes y un mostrador, se alza junto al camino. Cerca brilla suavemente un punto de guardado, junto a un pequeño cartel.");
beats += AmbBeat("amb_tundra3a", "room == room_tundra3A",
    "Un pequeño estanque helado apartado del camino principal, con la superficie salpicada de oscuros hoyos en el hielo. Han dejado una caña de pescar apoyada en la orilla, con el sedal extendido sobre la superficie helada.");
beats += AmbBeat("amb_tundra4", "room == room_tundra4",
    "Un tramo nevado del camino con otra garita de vigilancia a un lado. Cerca merodea un pequeño monstruo pájaro muy bien vestido, y unos cables de teléfono zumban débilmente por encima.");
beats += AmbBeat("amb_tundra5", "room == room_tundra5",
    "Aquí, junto al camino, hay un puesto de guardia construido como una pequeña caseta de perro. Una golosina para perros está al alcance, y una pequeña campana cuelga del mostrador.");
beats += AmbBeat("amb_tundra6", "room == room_tundra6",
    "Un tramo amplio y despejado donde una gran lámina de hielo resbaladizo cubre buena parte del suelo. Pisar el hielo te hace deslizarte hasta que vuelves a alcanzar suelo firme. Junto al camino hay un cartel.");
beats += AmbBeat("amb_tundra6a", "room == room_tundra6A",
    "Un pequeño y tranquilo claro apartado del camino. Aquí, entre la nieve amontonada, se alza un pequeño muñeco de nieve, redondo y paciente, como si esperara a que alguien le hablara.");
beats += AmbBeat("amb_tundra7", "room == room_tundra7",
    "Un campo nevado atravesado por un laberinto invisible de barreras eléctricas. En la nieve han quedado unas huellas que marcan un camino seguro a través de él: sigue las pisadas para cruzar sin recibir una descarga.");
beats += AmbBeat("amb_tundra8", "room == room_tundra8",
    "Aquí se abre un amplio campo nevado. Cerca de la entrada, un vendedor ofrece golosinas heladas, y al otro lado de la nieve se ha marcado un recorrido para un juego que consiste en hacer rodar una gran bola de nieve hacia un agujero lejano. Un árbol solitario se alza al borde del claro.");
beats += AmbBeat("amb_tundra8a", "room == room_tundra8A",
    "Un pequeño claro lateral con un par de casetas de perro una junto a la otra, y un cartel clavado entre ambas.");
beats += AmbBeat("amb_tundra9", "room == room_tundra9",
    "Un camino nevado donde una gran hoja de papel yace en el suelo, con un pasatiempo de palabras impreso (un crucigrama, o quizá un revoltijo de letras), abandonado en plena discusión.");
beats += AmbBeat("amb_tundra_spaghetti", "room == room_tundra_spaghetti",
    "Han montado una mesa sobre la nieve, con un plato de espaguetis congelados sobre ella junto a un microondas que no está enchufado a nada. Cerca brilla un punto de guardado, y una diminuta ratonera espera esperanzada en la pared del fondo.");
beats += AmbBeat("amb_tundra_snowpuzz", "room == room_tundra_snowpuzz",
    "Una gran sala de puzles. Una espesa capa de nieve cubre el suelo, con un lecho de pinchos encastrado en ella y altos árboles repartidos por doquier. Un camino seguro está oculto en la nieve, y un cartel cercano ofrece una pista para encontrar el modo de cruzar.");
beats += AmbBeat("amb_tundra_xoxosmall", "room == room_tundra_xoxosmall",
    "Una sala de puzles. En el suelo hay baldosas marcadas con una X. Pisa cada una para cambiarla a una O; conviértelas todas y los pinchos de delante bajan, abriendo el paso. Un cartel cercano explica las reglas.");
beats += AmbBeat("amb_tundra_xoxopuzz", "room == room_tundra_xoxopuzz",
    "Una versión más grande del puzle de X y O, con el suelo cubierto de baldosas marcadas y protegido por pinchos. Pisa cada X para convertirla en una O, y conviértelas todas para bajar los pinchos y pasar.");
beats += AmbBeat("amb_tundra_randoblock", "room == room_tundra_randoblock",
    "Una sala cuyo suelo es una cuadrícula de baldosas de colores vivos, con un interruptor encastrado en la pared del fondo que controla el puzle. A pesar de sus reglas de apariencia complicada, el camino se abre con bastante facilidad.");
beats += AmbBeat("amb_tundra_lesserdog", "room == room_tundra_lesserdog",
    "Aquí se alza otra garita de vigilancia, custodiada por un perro con armadura cuyo cuello puede estirarse imposiblemente. Cerca brilla un punto de guardado, y una caseta de perro se apoya contra la pared.");
beats += AmbBeat("amb_tundra_icehole", "room == room_tundra_icehole",
    "Una pequeña hornacina donde han construido dos toscas esculturas de nieve, imitaciones burdas de dos esqueletos. Un agujero en el suelo desciende aquí hacia algún lugar de abajo.");
beats += AmbBeat("amb_tundra_iceentrance", "room == room_tundra_iceentrance",
    "Una sala larga y cavernosa de hielo. Las zonas resbaladizas te hacen deslizarte al pisarlas, y en el suelo se abren agujeros dispuestos a hacerte caer al nivel inferior. Elige con cuidado un camino entre ellos.");
beats += AmbBeat("amb_tundra_iceexit_new", "room == room_tundra_iceexit_new",
    "El extremo más lejano de la caverna helada, con las paredes reluciendo de escarcha. A un lado se alza un monstruo peludo y con cornamenta, y la salida conduce hacia tierras más cálidas.");
beats += AmbBeat("amb_tundra_iceexit", "room == room_tundra_iceexit",
    "Un breve pasaje que sale de las cuevas de hielo. A lo lejos, en la distancia nevada, se distingue la diminuta silueta de una casa, que insinúa el pueblo que hay más adelante.");
beats += AmbBeat("amb_tundra_poffzone", "room == room_tundra_poffzone",
    "Una hondonada nevada salpicada de blandos montículos de nieve en polvo. Aquí hay una pequeña caseta de perro, y un perro husmea feliz entre la nieve amontonada.");
beats += AmbBeat("amb_tundra_dangerbridge", "room == room_tundra_dangerbridge",
    "Un largo puente de cuerda se extiende sobre un profundo abismo lleno de niebla. En el extremo más lejano han montado un despliegue de amenazantes artilugios: un perro, cañones, lanzas y un lanzallamas. Cruza el puente para continuar.");
beats += AmbBeat("amb_tundra_town", "room == room_tundra_town",
    "El pueblo de Snowdin: una larga y alegre calle de edificios de madera engalanados con luces de colores, con un árbol decorado brillando en su centro. Tiendas, una posada y un restaurante de aspecto cálido flanquean el camino, y los vecinos deambulan por la nieve. En el extremo del fondo se alzan dos buzones y una casa.");
beats += AmbBeat("amb_tundra_town2", "room == room_tundra_town2",
    "El extremo más tranquilo del pueblo, más allá del último de los edificios. Un lobo arroja bloques de hielo uno a uno a una cinta transportadora que se los lleva hacia el agua, y una familia de pequeños monstruos de baba se entretiene cerca.");
beats += AmbBeat("amb_tundra_dock", "room == room_tundra_dock",
    "Un embarcadero de madera a la orilla del agua, con las aguas oscuras lamiendo en silencio los tablones. Aquí espera un extraño barquero encapuchado, dispuesto a llevarte más lejos si se lo pides.");
beats += AmbBeat("amb_tundra_inn", "room == room_tundra_inn",
    "El acogedor vestíbulo de la posada de Snowdin. A un lado hay un mostrador de recepción, con la posadera esperando detrás, dispuesta a alquilarte una habitación para pasar la noche.");
beats += AmbBeat("amb_tundra_inn_2f", "room == room_tundra_inn_2f",
    "Una pequeña y acogedora habitación en el piso de arriba de la posada. Una cama espera de forma tentadora; un buen descanso aquí curará tus heridas.");
beats += AmbBeat("amb_tundra_grillby", "room == room_tundra_grillby",
    "El Grillby's, el cálido y animado restaurante del pueblo. Reservados y mesas llenan el local, con un grupo de perros y otros clientes habituales reunidos, y tras la barra se encuentra el dueño: un monstruo callado hecho de fuego viviente.");
beats += AmbBeat("amb_tundra_library", "room == room_tundra_library",
    "La biblioteca del pueblo, aunque el cartel de fuera la deletrea como bibloteca. Hileras de estanterías y mesas de lectura llenan la sala, y unos cuantos monstruos estudiosos levantan la vista de su trabajo cuando entras.");
beats += AmbBeat("amb_tundra_garage", "room == room_tundra_garage",
    "Un garaje abarrotado. Una cama para perro, un cuenco de comida y un juguete bien mordisqueado están repartidos por el suelo, y un extraño artilugio con barrotes se apoya contra una pared.");
beats += AmbBeat("amb_tundra_sanshouse", "room == room_tundra_sanshouse",
    "La sala principal de la casa de los hermanos esqueleto, acogedora y bien vivida. Un sofá mira hacia una televisión, una cocina se abre a la derecha, y unas puertas conducen a los dormitorios.");
beats += AmbBeat("amb_tundra_paproom", "room == room_tundra_paproom",
    "Un dormitorio que pertenece al hermano más alto. Una cama con forma de coche de carreras se apoya contra una pared, junto con un ordenador, una estantería, una caja de huesos y una figura de acción posando sobre una pequeña mesa.");
beats += AmbBeat("amb_tundra_sansroom", "room == room_tundra_sansroom",
    "El dormitorio del hermano más bajo, y un desorden espectacular. Ropa y basura cubren el suelo, un tornado de basura autosuficiente gira en silencio en un rincón, y una cinta de correr yace enterrada y sin usar.");
beats += AmbBeat("amb_tundra_sansroom_dark", "room == room_tundra_sansroom_dark",
    "Una sala amplia y completamente a oscuras. Algo aguarda en la negrura de delante, justo fuera de la vista.");
beats += AmbBeat("amb_tundra_sansbasement", "room == room_tundra_sansbasement",
    "Un taller oculto tras una puerta cerrada con llave, polvoriento y largamente olvidado. Hay papeles extraños clavados en las paredes, y un gran objeto reposa bajo una tela en el rincón.");
// ---- Waterfall: the deep blue caverns (verified against _rooms_all.txt, room_water*) ----
beats += AmbBeat("amb_water1", "room == room_water1",
    "Dejas atrás la nieve y entras en Waterfall, una vasta caverna de un azul intenso. El agua gotea y resuena por todas partes, plantas luminosas iluminan la penumbra, y el aire se vuelve cálido y húmedo.");
beats += AmbBeat("amb_water2", "room == room_water2",
    "Un saliente tranquilo donde se alza una garita de vigilancia de madera, sin vigilar y sospechosamente informal. Cerca brilla un punto de guardado, y aquí crece una única flor de eco azul, que repite en voz baja lo último que le susurraron.");
beats += AmbBeat("amb_water3", "room == room_water3",
    "Una cascada se derrama por una pared hasta un canal de agua en movimiento. Una gran roca reposa en la orilla; empújala hacia la corriente para bloquearla y poder cruzar. Cerca hay un cartel y una flor de eco.");
beats += AmbBeat("amb_water3a", "room == room_water3A",
    "Una pequeña hornacina iluminada por un par de altos hongos azules luminosos.");
beats += AmbBeat("amb_water4", "room == room_water4",
    "Un espeso matorral de hierba alta crece en el centro de esta sala, lo bastante alto como para esconderse en él. En un saliente muy por encima, una alta figura con armadura observa, completamente inmóvil. Al otro lado brilla un punto de guardado.");
beats += AmbBeat("amb_water_bridgepuzz1", "room == room_water_bridgepuzz1",
    "Un puzle de semillas-puente. Racimos de semillas flotan en el agua; camina hacia ellas para juntarlas, y allí donde se agrupan brotan puentes sólidos de nenúfares que puedes cruzar. Un cartel cercano lo explica.");
beats += AmbBeat("amb_water5", "room == room_water5",
    "Un tramo de agua más grande sembrado de semillas-puente a la deriva. Junta las semillas para hacer crecer puentes de nenúfares sobre el agua. Hongos luminosos y una flor con forma de campana iluminan la sala, y un cartel ofrece una pista.");
beats += AmbBeat("amb_water5a", "room == room_water5A",
    "Una pequeña sala lateral junto al agua, iluminada por el suave resplandor de una flor de eco.");
beats += AmbBeat("amb_water6", "room == room_water6",
    "Una sala sobrecogedora. El alto techo reluce con incontables lucecitas, como un cielo nocturno cuajado de estrellas, aunque solo son gemas en la roca. Aquí crecen flores de eco, cada una susurrando aún un deseo que alguien exhaló en ella un día, y un telescopio invita a mirar hacia arriba.");
beats += AmbBeat("amb_water7", "room == room_water7",
    "Una pasarela de madera sobre aguas oscuras, donde un pequeño y quisquilloso monstruo amante del agua friega afanosamente los tablones. Una hilera de carteles bordea el camino.");
beats += AmbBeat("amb_water8", "room == room_water8",
    "Una larga pasarela de madera que se extiende sobre aguas abiertas.");
beats += AmbBeat("amb_water9", "room == room_water9",
    "Una pasarela con un matorral de hierba alta que crece espeso en su borde, justo lo bastante frondoso como para esconderse.");
beats += AmbBeat("amb_water_savepoint1", "room == room_water_savepoint1",
    "Un rincón tranquilo con un punto de guardado, una flor de eco luminosa y una pequeña ratonera desgastada en la pared.");
beats += AmbBeat("amb_water11", "room == room_water11",
    "Una sala en penumbra donde partículas de luz flotan en el aire. Aquí hay un telescopio para contemplar el agua, y cerca se encuentra una pequeña garita de vigilancia.");
beats += AmbBeat("amb_water_nicecream", "room == room_water_nicecream",
    "Una gruta de hongos luminosos donde el vendedor de Nice Cream ha montado su carrito, ofreciendo golosinas heladas con palabras amables escritas en los envoltorios.");
beats += AmbBeat("amb_water12", "room == room_water12",
    "Una vasta y hermosa caverna que brilla de un azul intenso. Agua reluciente se derrama por las paredes, plantas luminosas se mecen a la deriva, y flores de eco susurran aquí y allá. Es una de las vistas más bellas de todo el Subsuelo.");
beats += AmbBeat("amb_water_shoe", "room == room_water_shoe",
    "Un pequeño claro entre hongos luminosos, con un matorral de hierba alta en su centro.");
beats += AmbBeat("amb_water_bird", "room == room_water_bird",
    "Un saliente al borde de un amplio abismo. Aquí espera un ave grande y apacible, dispuesta a llevar al otro lado a quien sea lo bastante pequeño.");
beats += AmbBeat("amb_water_onionsan", "room == room_water_onionsan",
    "Un canal largo y poco profundo donde el agua ha bajado preocupantemente. Una gran criatura marina, blanda y amistosa, se entretiene en las aguas someras, encantada de tener con quien hablar.");
beats += AmbBeat("amb_water14", "room == room_water14",
    "Un saliente lluvioso donde un canto suave y afligido flota en el aire. Aquí flota un tímido monstruo con aspecto de pez, medio escondido tras su propia canción, y hay carteles a lo largo del camino.");
beats += AmbBeat("amb_water_piano", "room == room_water_piano",
    "Una sala con un viejo piano contra la pared. Tocar la melodía correcta (la tonada que se insinúa en otros puntos de las cavernas) abrirá el camino hacia una recompensa oculta.");
beats += AmbBeat("amb_water_dogroom", "room == room_water_dogroom",
    "Una sala silenciosa, como un santuario. Aquí reposa sobre un pedestal un curioso artefacto antiguo, esperando a que lo cojan.");
beats += AmbBeat("amb_water_statue", "room == room_water_statue",
    "Una estatua de piedra permanece sola bajo una lluvia interminable, callada e inmóvil. Cerca hay una caja de paraguas; resguarda la estatua de la lluvia y revelará la dulce música que siempre estuvo destinada a tocar.");
beats += AmbBeat("amb_water_prewaterfall", "room == room_water_prewaterfall",
    "Aquí empieza la lluvia. Junto a un cartel hay una caja de paraguas: coge uno para no mojarte en la larga caminata que te espera.");
beats += AmbBeat("amb_water_waterfall", "room == room_water_waterfall",
    "Un largo camino bajo una lluvia constante, con cascadas cayendo como cortinas a ambos lados. Es un paseo hermoso y melancólico.");
beats += AmbBeat("amb_water_waterfall2", "room == room_water_waterfall2",
    "Un pasaje alto y lluvioso que asciende, con una única flor de eco brillando suavemente por el camino.");
beats += AmbBeat("amb_water_waterfall3", "room == room_water_waterfall3",
    "Un saliente lluvioso con una amplia vista panorámica: al otro lado del agua, a lo lejos, se alza el gran castillo gris del Rey, la meta de todo este largo viaje.");
beats += AmbBeat("amb_water_waterfall4", "room == room_water_waterfall4",
    "Un camino lluvioso donde una caja de paraguas espera a ser rellenada. Cerca, un musculoso monstruo con aspecto de caballo marca músculo y guiña un ojo.");
beats += AmbBeat("amb_water_preundyne", "room == room_water_preundyne",
    "Un saliente tranquilo bajo la lluvia, con un punto de guardado. El camino de delante asciende a un alto puente de madera.");
beats += AmbBeat("amb_water_undynebridge", "room == room_water_undynebridge",
    "Un puente alto y estrecho sobre un profundo abismo, azotado por la lluvia. Este es un terreno peligroso: la figura con armadura caza aquí, arrojando lanzas desde la oscuridad. No dejes de moverte.");
beats += AmbBeat("amb_water_undynebridgeend", "room == room_water_undynebridgeend",
    "El extremo más lejano del puente, acorralado y sin ningún sitio al que huir mientras las lanzas llueven desde la penumbra.");
beats += AmbBeat("amb_water_trashzone1", "room == room_water_trashzone1",
    "Un vertedero en penumbra al pie de las cascadas, donde acaba todo lo que cae al Subsuelo. Montones de basura yacen en aguas someras, y un monstruo amante del agua trastea entre ellos.");
beats += AmbBeat("amb_water_trashsavepoint", "room == room_water_trashsavepoint",
    "Una zona del vertedero con un punto de guardado, en medio de aguas someras entre la basura amontonada.");
beats += AmbBeat("amb_water_trashzone2", "room == room_water_trashzone2",
    "Una cámara alta y repleta de chatarra. Entre los montones de basura y una vieja nevera oxidada flota un muñeco grumoso y furioso, con ganas de pelea.");
beats += AmbBeat("amb_water_friendlyhub", "room == room_water_friendlyhub",
    "Un tramo más tranquilo de Waterfall con un punto de guardado y un cartel. Cerca hay acogedoras casitas, y un amistoso monstruo con aspecto de almeja parlotea junto al camino.");
beats += AmbBeat("amb_water_undyneyard", "room == room_water_undyneyard",
    "El patio ante una casa llamativa con forma de pez. Una puerta espera a que llamen: este es el hogar de la fiera capitana de la guardia del Subsuelo.");
beats += AmbBeat("amb_water_undynehouse", "room == room_water_undynehouse",
    "El interior de la casa con forma de pez de Undyne, cálida y con mucha personalidad. Un cajón se apoya contra una pared, curiosamente repleto de huesos, y hay estanterías y recuerdos que curiosear.");
beats += AmbBeat("amb_water_blookyard", "room == room_water_blookyard",
    "Un patio tranquilo con un par de casitas. Aquí flota un pequeño fantasma blanco, tenue y tímido.");
beats += AmbBeat("amb_water_blookhouse", "room == room_water_blookhouse",
    "El hogar del fantasma, pequeño y algo melancólico. Un ordenador zumba en el rincón, cerca hay un frigorífico, y una pila de CD de música espera junto a un sitio en el suelo donde podéis tumbaros y sentiros como basura juntos.");
beats += AmbBeat("amb_water_hapstablook", "room == room_water_hapstablook",
    "Una casa vecina, vacía y silenciosa, con las estanterías llenas de viejos diarios que dejó atrás quienquiera que soñó aquí una vez.");
beats += AmbBeat("amb_water_farm", "room == room_water_farm",
    "Una pequeña y húmeda granja de caracoles. Los caracoles avanzan lentamente por su corral, y una pequeña pista de carreras espera a quien le apetezca apostar por el más rápido.");
beats += AmbBeat("amb_water_prebird", "room == room_water_prebird",
    "Un saliente cubierto de hierba, con un espeso matorral de hierba alta y unos cuantos carteles a lo largo del camino.");
beats += AmbBeat("amb_water_shop", "room == room_water_shop",
    "Una acogedora tienda excavada en la roca, atendida por una alegre tortuga anciana que ha sido testigo de toda la larga historia del Subsuelo.");
beats += AmbBeat("amb_water_dock", "room == room_water_dock",
    "Un pequeño embarcadero a la orilla del agua, donde el barquero encapuchado espera para llevarte más lejos si lo deseas.");
beats += AmbBeat("amb_water15", "room == room_water15",
    "Una caverna oscura que titila con luciérnagas luminosas y ecos a la deriva, con aguas someras encharcadas por el suelo.");
beats += AmbBeat("amb_water16", "room == room_water16",
    "Una sala oscura iluminada solo por racimos de hongos luminosos. Al acercarte a un hongo, este resplandece con fuerza, iluminando el camino de uno al siguiente.");
beats += AmbBeat("amb_water_temvillage", "room == room_water_temvillage",
    "La Aldea Temmie, una acogedora madriguera llena de pequeñas y excitables criaturas mitad gato mitad perro que parlotean todas a la vez. Aquí hay una tienda, un punto de guardado y una Temmie que, por el precio adecuado, te venderá la oportunidad de pagarle la universidad.");
beats += AmbBeat("amb_water17", "room == room_water17",
    "Una sala completamente a oscuras. Se puede coger un farol y llevarlo para iluminar un pequeño círculo a tu alrededor; piedras luminosas y otros faroles ayudan a señalar el camino seguro.");
beats += AmbBeat("amb_water18", "room == room_water18",
    "Un camino en penumbra con hierba alta y aguas someras. Más adelante, la figura con armadura atraviesa de un golpe una pared de bloques en furiosa persecución.");
beats += AmbBeat("amb_water19", "room == room_water19",
    "Un pozo alto y luminoso flanqueado por flores de eco susurrantes, con un punto de guardado a media altura. Las flores de aquí transmiten una advertencia inquietante.");
beats += AmbBeat("amb_water20", "room == room_water20",
    "Un pasaje corto y en penumbra que sigue adelante a través de las cavernas.");
beats += AmbBeat("amb_water21", "room == room_water21",
    "Una pequeña sala de puzles, con una caja de interruptores encastrada en la pared.");
beats += AmbBeat("amb_water13", "room == room_water13",
    "Otro puzle de semillas-puente, en medio de un matorral de hierba alta. Junta las semillas a la deriva para hacer brotar puentes de nenúfares sobre el agua.");
beats += AmbBeat("amb_water_mushroom", "room == room_water_mushroom",
    "Una sala pequeña y tranquila con un cartel que leer y un curioso monstruo con aspecto de hongo.");
beats += AmbBeat("amb_water_undynefinal", "room == room_water_undynefinal",
    "Una solitaria cima de acantilado bañada en luz dorada al borde de Waterfall. La figura con armadura planta cara aquí, y ya no queda ningún sitio al que huir: solo darte la vuelta y hacerle frente.");
beats += AmbBeat("amb_water_undynefinal2", "room == room_water_undynefinal2",
    "Un camino que huye hacia un muro de calor creciente, con la figura con armadura persiguiéndote sin tregua.");
beats += AmbBeat("amb_water_undynefinal3", "room == room_water_undynefinal3",
    "La frontera entre Waterfall y Hotland, marcada por un cartel. El aire aquí se vuelve de repente abrasadoramente caluroso.");
// ---- Hotland: red rock over lava, Alphys's lab, MTT Resort (verified against _rooms_all.txt) ----
beats += AmbBeat("amb_fire1", "room == room_fire1",
    "Llegas a Hotland. El aire es un muro de calor seco, la roca brilla al rojo, y muy abajo se agita un río de lava. Cerca hay una garita de vigilancia con su centinela profundamente dormido, y un dispensador ofrece un vaso de agua fría.");
beats += AmbBeat("amb_fire2", "room == room_fire2",
    "Un saliente caluroso y estrecho sobre la lava. Aquí hay un dispensador de agua, y un parlanchín monstruo con aspecto de almeja se entretiene junto al camino.");
beats += AmbBeat("amb_fire_prelab", "room == room_fire_prelab",
    "Un saliente ante un gran laboratorio gris construido en la roca, con la puerta sellada. Cerca brilla un punto de guardado.");
beats += AmbBeat("amb_fire_dock", "room == room_fire_dock",
    "Un pequeño embarcadero a la orilla de la lava, donde el barquero encapuchado espera para llevarte más lejos si lo deseas.");
beats += AmbBeat("amb_fire_lab1", "room == room_fire_lab1",
    "El interior en penumbra del laboratorio de la Científica Real. Un monitor gigantesco domina una pared, y la sala está abarrotada de instrumentos, un frigorífico y sacos de comida para perros. Reina un silencio inquietante.");
beats += AmbBeat("amb_fire_lab2", "room == room_fire_lab2",
    "Una planta inferior del laboratorio, con escaleras mecánicas zumbando a ambos lados. Las estanterías y vitrinas de aquí están repletas de la no tan secreta colección de dibujos animados y figuritas de la científica.");
beats += AmbBeat("amb_fire3", "room == room_fire3",
    "Un cruce caluroso de roca roja, con tuberías recorriendo el suelo y avisos de advertencia colgados por doquier.");
beats += AmbBeat("amb_fire5", "room == room_fire5",
    "Un pozo alto entrecruzado por cintas transportadoras en movimiento, con chorros de vapor azul que te impulsan por encima de los huecos. Súbete a las cintas y a los respiraderos para escalarlo. Aquí deambula un pequeño monstruo con aspecto de volcán.");
beats += AmbBeat("amb_fire6", "room == room_fire6",
    "Una sala de respiraderos de vapor. Pisar un respiradero te lanza en la dirección en que sopla; encadena los chorros para cruzar la lava hasta el otro lado. Cerca brilla un punto de guardado.");
beats += AmbBeat("amb_fire6a", "room == room_fire6A",
    "Una sala más pequeña de cintas transportadoras y un respiradero de vapor, con un ardiente monstruo con aspecto de pájaro flotando cerca.");
beats += AmbBeat("amb_fire_lasers1", "room == room_fire_lasers1",
    "Un puzle de láseres. Haces de luz bloquean el paso: los haces naranjas debes atravesarlos en movimiento; los azules solo hacen daño si te mueves, así que quédate quieto para pasarlos. Un interruptor al final los enciende o apaga todos.");
beats += AmbBeat("amb_fire7", "room == room_fire7",
    "Una sala de respiraderos de vapor con una puerta de tarjeta bloqueada en el centro. Cruza montado en los chorros de vapor para llegar al otro lado.");
beats += AmbBeat("amb_fire8", "room == room_fire8",
    "Una sala calurosa iluminada por una alta torre-faro, con engranajes girando en las paredes y un láser azul bloqueando parte del camino.");
beats += AmbBeat("amb_fire9", "room == room_fire9",
    "Una pequeña sala calurosa, con una torre-faro brillando en su cima y engranajes encastrados en las paredes.");
beats += AmbBeat("amb_fire_shootguy_1", "room == room_fire_shootguy_1",
    "Un puzle de disparos. Un cañón se encuentra en la parte inferior de la sala y unas cajas naranjas flotan por encima; alinea el cañón y dispara para volar las cajas y despejar el camino.");
beats += AmbBeat("amb_fire_shootguy_2", "room == room_fire_shootguy_2",
    "Otro puzle de disparos: apunta con el cañón y dispara para apartar las cajas naranjas flotantes.");
beats += AmbBeat("amb_fire_shootguy_3", "room == room_fire_shootguy_3",
    "Un puzle de disparos más grande, con más cajas naranjas que volar con el cañón antes de poder pasar.");
beats += AmbBeat("amb_fire_shootguy_4", "room == room_fire_shootguy_4",
    "Un puzle de disparos: alinea el cañón y dispara para despejar de tu camino las cajas naranjas flotantes.");
beats += AmbBeat("amb_fire_shootguy_5", "room == room_fire_shootguy_5",
    "Un puzle de disparos situado dentro del CORE, con cajas naranjas que apartar con el cañón.");
beats += AmbBeat("amb_fire_turn", "room == room_fire_turn",
    "Un rincón caluroso donde los chorros de vapor te hacen rebotar por la curva.");
beats += AmbBeat("amb_fire4", "room == room_fire4",
    "Una gran sala de respiraderos de vapor, con chorros que te lanzan hacia la derecha de saliente en saliente por encima de la lava.");
beats += AmbBeat("amb_fire_cookingshow", "room == room_fire_cookingshow",
    "Un escenario de programa de cocina muy iluminado, con su encimera y deslumbrantes luces de estudio. Aquí, una estrella robótica de la televisión está montando un programa de cocina en directo.");
beats += AmbBeat("amb_fire_savepoint1", "room == room_fire_savepoint1",
    "Un saliente con un punto de guardado y una amplia vista del CORE, el gran motor luminoso del Subsuelo, alzándose a lo lejos.");
beats += AmbBeat("amb_fire_hotdog", "room == room_fire_hotdog",
    "Una garita de vigilancia donde cierto esqueleto perezoso vende perritos calientes, con un olor cálido y grasiento flotando en el aire.");
beats += AmbBeat("amb_fire_walkandbranch", "room == room_fire_walkandbranch",
    "Un caluroso camino que se bifurca sobre la lava, con avisos de advertencia colgados a lo largo y un monstruo con aspecto de avión flotando cerca.");
beats += AmbBeat("amb_fire_sorry", "room == room_fire_sorry",
    "Una pequeña sala apartada con un cartel de clase de arte en la pared.");
beats += AmbBeat("amb_fire_apron", "room == room_fire_apron",
    "Un saliente caluroso donde un monstruo que hace olas se entretiene junto al camino.");
beats += AmbBeat("amb_fire10", "room == room_fire10",
    "Una sala de cintas transportadoras con una fila de tres interruptores que hay que colocar en el patrón correcto antes de que se abra el paso.");
beats += AmbBeat("amb_fire_rpuzzle", "room == room_fire_rpuzzle",
    "Un puzle de respiraderos de vapor y cintas transportadoras. Rebota entre los chorros y súbete a las cintas para mover los bloques y cruzar.");
beats += AmbBeat("amb_fire_mewmew2", "room == room_fire_mewmew2",
    "Una sala calurosa con un punto de guardado y una pequeña ratonera desgastada en la pared.");
beats += AmbBeat("amb_fire_boysnightout", "room == room_fire_boysnightout",
    "Una sala calurosa de respiraderos de vapor, con un ardiente monstruo con aspecto de pájaro rondando entre los chorros.");
beats += AmbBeat("amb_fire_newsreport", "room == room_fire_newsreport",
    "Un plató donde la estrella robótica de la televisión está grabando un informativo en directo, con láseres y respiraderos de vapor montados alrededor del escenario.");
beats += AmbBeat("amb_fire_coreview2", "room == room_fire_coreview2",
    "Un saliente con otra magnífica vista del CORE brillando a lo lejos.");
beats += AmbBeat("amb_fire_spidershop", "room == room_fire_spidershop",
    "Un acogedor rincón cubierto de telarañas, donde una alegre chica araña regenta una venta de dulces. Deja unas monedas y las arañas te venderán un dulce: todo hecho para arañas, por arañas.");
beats += AmbBeat("amb_fire_walkandbranch2", "room == room_fire_walkandbranch2",
    "Un gran laberinto de respiraderos de vapor que asciende, con chorros de vapor que te lanzan de un saliente al siguiente.");
beats += AmbBeat("amb_fire_conveyorlaser", "room == room_fire_conveyorlaser",
    "Una sala que combina cintas transportadoras en movimiento con haces de láser azul: quédate perfectamente quieto al atravesar los haces azules mientras las cintas te arrastran. A un lado brilla una flor de eco.");
beats += AmbBeat("amb_fire_preshootguy4", "room == room_fire_preshootguy4",
    "Una pequeña sala calurosa iluminada por una torre-faro, con un par de monstruos infantiles con aspecto de gema merodeando por aquí.");
beats += AmbBeat("amb_fire_savepoint2", "room == room_fire_savepoint2",
    "Un saliente caluroso cubierto de seda de araña, con un punto de guardado brillando suavemente aquí.");
beats += AmbBeat("amb_fire_spider", "room == room_fire_spider",
    "Un largo pasillo cubierto de telarañas en lo profundo del territorio de las arañas. La seda pegajosa ralentiza tus pasos, las arañas observan desde lo alto, y su señora no anda lejos.");
beats += AmbBeat("amb_fire_pacing", "room == room_fire_pacing",
    "Una pequeña sala con un cartel que leer y un monstruo que se pasea de un lado a otro.");
beats += AmbBeat("amb_fire_multitile", "room == room_fire_multitile",
    "Un enorme puzle de baldosas de colores: un largo suelo de baldosas de muchos colores, cada color con su propia regla (algunos seguros, otros no) que una voz anuncia antes de que cruces. Cerca espera un pequeño monstruo volcán.");
beats += AmbBeat("amb_fire_hotelfront_1", "room == room_fire_hotelfront_1",
    "La gran entrada del MTT Resort, un ostentoso hotel excavado sobre la lava. El vendedor de Nice Cream ha montado su carrito junto a las puertas.");
beats += AmbBeat("amb_fire_hotelfront_2", "room == room_fire_hotelfront_2",
    "La entrada hacia las puertas del hotel, con una mullida alfombra roja bajo tus pies.");
beats += AmbBeat("amb_fire_hotellobby", "room == room_fire_hotellobby",
    "El opulento vestíbulo del MTT Resort, chillón y dorado. Una fuente con la forma de la estrella robótica borbotea en el centro, un recepcionista espera en el mostrador, y elegantes monstruos huéspedes deambulan por allí. Cerca brilla un punto de guardado, y un ascensor aguarda listo.");
beats += AmbBeat("amb_fire_restaurant", "room == room_fire_restaurant",
    "El elegante restaurante del hotel, con las mesas puestas con esmero y macetas a lo largo de las paredes, y huéspedes cenando en silencio.");
beats += AmbBeat("amb_fire_hoteldoors", "room == room_fire_hoteldoors",
    "Un pasillo de puertas de habitaciones, con un fatigado conserje de baba fregando el suelo.");
beats += AmbBeat("amb_fire_hotelbed", "room == room_fire_hotelbed",
    "Una lujosa habitación de hotel, con una gran cama blanda esperando: un descanso aquí te restablecerá.");
beats += AmbBeat("amb_fire_precore", "room == room_fire_precore",
    "Un pozo oscuro que desciende hacia el CORE, con el zumbido de la maquinaria pesada subiendo desde abajo.");
beats += AmbBeat("amb_fire_core1", "room == room_fire_core1",
    "La entrada al CORE, la vasta central eléctrica del Subsuelo. Las oscuras paredes de metal brillan con tiras de luz azul, y el aire vibra de energía. Aquí hay un ascensor.");
beats += AmbBeat("amb_fire_core2", "room == room_fire_core2",
    "Una cámara del CORE iluminada de un azul inquietante, con pequeñas llamas titilando en braseros a lo largo de las paredes.");
beats += AmbBeat("amb_fire_core3", "room == room_fire_core3",
    "Una sala del CORE flanqueada por tótems luminosos, con una gran puerta en la pared del fondo.");
beats += AmbBeat("amb_fire_core4", "room == room_fire_core4",
    "Una sala del CORE bloqueada por haces de láser, con un interruptor cerca para alternarlos: atraviesa los naranjas, quédate quieto para los azules.");
beats += AmbBeat("amb_fire_core5", "room == room_fire_core5",
    "Una pequeña cámara del CORE que brilla con tótems y luz azul.");
beats += AmbBeat("amb_fire_core_freebattle", "room == room_fire_core_freebattle",
    "Una pequeña cámara del CORE donde un enemigo sombrío acecha en la luz azul.");
beats += AmbBeat("amb_fire_core_laserfun", "room == room_fire_core_laserfun",
    "Un largo salón del CORE atravesado por una sucesión de haces de láser azules y naranjas. Atraviesa los naranjas, quédate inmóvil para los azules.");
beats += AmbBeat("amb_fire_core_branch", "room == room_fire_core_branch",
    "Un cruce del CORE con un punto de guardado y carteles, con tiras de luz brillantes señalando varios caminos. Es fácil desorientarse aquí.");
beats += AmbBeat("amb_fire_core_bottomleft", "room == room_fire_core_bottomleft",
    "Un pasillo del CORE con cintas transportadoras recorriendo el suelo y tiras de luz azul brillando en las oscuras paredes.");
beats += AmbBeat("amb_fire_core_left", "room == room_fire_core_left",
    "Un pasillo del CORE que se bifurca hacia la izquierda, con tótems luminosos encastrados en las paredes.");
beats += AmbBeat("amb_fire_core_topleft", "room == room_fire_core_topleft",
    "Un cruce de pasillos del CORE, con tiras de luz azul flanqueando las oscuras paredes de metal.");
beats += AmbBeat("amb_fire_core_top", "room == room_fire_core_top",
    "Un pasillo del CORE cerca de lo alto del laberinto, con carteles colgados y tiras de luz brillando.");
beats += AmbBeat("amb_fire_core_topright", "room == room_fire_core_topright",
    "Un cruce de pasillos del CORE que zumba de energía, con luz azul recorriendo las paredes.");
beats += AmbBeat("amb_fire_core_right", "room == room_fire_core_right",
    "Un pasillo del CORE donde un reluciente campo de fuerza sella una de las puertas.");
beats += AmbBeat("amb_fire_core_bottomright", "room == room_fire_core_bottomright",
    "Un recodo del CORE donde una pasarela salva la oscura caída, con tótems luminosos montando guardia.");
beats += AmbBeat("amb_fire_core_center", "room == room_fire_core_center",
    "El cruce central del CORE, con caminos que se bifurcan en todas direcciones y un enemigo sombrío merodeando cerca.");
beats += AmbBeat("amb_fire_core_treasureleft", "room == room_fire_core_treasureleft",
    "Una hornacina del CORE apartada del laberinto, que alberga algo que merece la pena coger y un monstruo con quien hablar.");
beats += AmbBeat("amb_fire_core_treasureright", "room == room_fire_core_treasureright",
    "Una hornacina del CORE escondida fuera del laberinto, que alberga una pequeña recompensa.");
beats += AmbBeat("amb_fire_core_warrior", "room == room_fire_core_warrior",
    "Un salón del CORE donde un fiero monstruo guerrero bloquea el paso, con un interruptor reluciendo en el extremo del fondo.");
beats += AmbBeat("amb_fire_core_bridge", "room == room_fire_core_bridge",
    "Un largo puente del CORE que cruza un oscuro abismo, con tótems luminosos flanqueando las barandillas y luz derramándose por encima.");
beats += AmbBeat("amb_fire_core_premett", "room == room_fire_core_premett",
    "Una cámara del CORE con un punto de guardado y una gran puerta delante, con un ascensor esperando a un lado.");
beats += AmbBeat("amb_fire_core_metttest", "room == room_fire_core_metttest",
    "Un alto y dramático escenario del CORE: el escenario de una gran confrontación con la estrella robótica.");
beats += AmbBeat("amb_fire_core_final", "room == room_fire_core_final",
    "El extremo más lejano del CORE, con un ascensor esperando para llevarte arriba, fuera de las profundidades.");
beats += AmbBeat("amb_fire_elevator", "room == room_fire_elevator || room == room_fire_finalelevator || room == room_fire_labelevator",
    "Una pequeña cabina de ascensor, con su panel de control brillando junto a la puerta.");
beats += AmbBeat("amb_fire_elevator_gems", "room == room_fire_elevator_r1 || room == room_fire_elevator_r2 || room == room_fire_elevator_r3 || room == room_fire_elevator_l1 || room == room_fire_elevator_l2 || room == room_fire_elevator_l3",
    "Una pequeña cabina de ascensor, con el número de planta brillando en un cartel junto a la puerta.");
// ---- The True Lab: dark and half-forgotten beneath the lab (verified against _rooms_all.txt) ----
beats += AmbBeat("amb_truelab_elevatorinside", "room == room_truelab_elevatorinside",
    "Un ascensor en penumbra que te ha llevado a un lugar al que nunca debías ir.");
beats += AmbBeat("amb_truelab_elevator", "room == room_truelab_elevator",
    "Un rellano oscuro a la salida del ascensor, con una pesada puerta de laboratorio delante.");
beats += AmbBeat("amb_truelab_hall1", "room == room_truelab_hall1",
    "Un oscuro pasillo de laboratorio, con las paredes cubiertas de monitores zumbantes que arrojan un resplandor enfermizo.");
beats += AmbBeat("amb_truelab_hub", "room == room_truelab_hub",
    "Un sombrío nudo donde confluyen varias puertas de laboratorio. Aquí brilla un punto de guardado, hay oscuras plantas marchitas en sus macetas, y una nota rasgada yace en el suelo.");
beats += AmbBeat("amb_truelab_hall2", "room == room_truelab_hall2",
    "Un corto y oscuro pasillo de laboratorio, con un monitor parpadeando en la pared.");
beats += AmbBeat("amb_truelab_operatingroom", "room == room_truelab_operatingroom",
    "Un lúgubre quirófano medio perdido en la niebla, con mesas vacías y pilas mugrientas alineadas en la penumbra.");
beats += AmbBeat("amb_truelab_redlever", "room == room_truelab_redlever",
    "Una sala oscura con una palanca de color en la pared (una de varias que hay que accionar) y una nota rasgada tirada cerca.");
beats += AmbBeat("amb_truelab_bluelever", "room == room_truelab_bluelever",
    "Una sala oscura con una palanca de color en la pared y una nota rasgada en el suelo.");
beats += AmbBeat("amb_truelab_greenlever", "room == room_truelab_greenlever",
    "Una sala oscura con una palanca de color y una nota rasgada tirada a su lado.");
beats += AmbBeat("amb_truelab_prebed", "room == room_truelab_prebed",
    "Un oscuro pasillo flanqueado por monitores luminosos.");
beats += AmbBeat("amb_truelab_bedroom", "room == room_truelab_bedroom",
    "Un dormitorio en penumbra con camas vacías envueltas en niebla. Algo observa desde entre ellas. En un rincón brilla un punto de guardado, y una llave reposa sobre una de las camas.");
beats += AmbBeat("amb_truelab_mirror", "room == room_truelab_mirror",
    "Un largo y oscuro salón flanqueado por espejos. En la niebla del extremo del fondo, una extraña forma cambiante espera y observa.");
beats += AmbBeat("amb_truelab_hall3", "room == room_truelab_hall3",
    "Otro oscuro pasillo de laboratorio, con monitores brillando débilmente a lo largo de las paredes.");
beats += AmbBeat("amb_truelab_shower", "room == room_truelab_shower",
    "Una pequeña sala con una cortina de ducha corrida, con algo acechando tras ella.");
beats += AmbBeat("amb_truelab_determination", "room == room_truelab_determination",
    "Una sala oscura construida en torno a una extraña máquina de extracción. Aquí brilla un punto de guardado, aunque este parece rara e inquietantemente vivo.");
beats += AmbBeat("amb_truelab_tv", "room == room_truelab_tv",
    "Una sala oscura con un viejo televisor y una palanca de color, con una nota rasgada en el suelo.");
beats += AmbBeat("amb_truelab_cooler", "room == room_truelab_cooler",
    "Una cámara frigorífica, con hileras de oscuros frigoríficos zumbando bajo ventiladores en marcha. Algo se agita entre ellos.");
beats += AmbBeat("amb_truelab_fan", "room == room_truelab_fan",
    "Una sala amurallada con grandes ventiladores giratorios, con la niebla enroscándose entre ellos. Una forma se mueve en la bruma.");
beats += AmbBeat("amb_truelab_prepower", "room == room_truelab_prepower",
    "Un oscuro pasillo de monitores que conduce hacia la sala de energía.");
beats += AmbBeat("amb_truelab_power", "room == room_truelab_power",
    "Una pequeña sala que alberga el interruptor principal de energía, listo para volver a encender las luces.");
beats += AmbBeat("amb_truelab_castle_elevator", "room == room_truelab_castle_elevator",
    "Un ascensor listo para llevarte arriba, fuera del verdadero laboratorio.");
// ---- New Home: the King's grey city, Asgore's house, the castle (verified against _rooms_all.txt) ----
beats += AmbBeat("amb_castle_elevatorout", "room == room_castle_elevatorout",
    "Sales del ascensor a New Home, la gris ciudad del Rey en las alturas. Cerca brilla un punto de guardado, y una música lenta y afligida flota en el aire.");
beats += AmbBeat("amb_castle_precastle", "room == room_castle_precastle",
    "Una larga y gris aproximación hacia el castillo, con las ruinas de un hogar muy, muy antiguo asomando en silencio más adelante.");
beats += AmbBeat("amb_castle_front", "room == room_castle_front",
    "La fachada del castillo, alta, gris y silenciosa. Aquí brilla un punto de guardado.");
beats += AmbBeat("amb_kitchen", "room == room_kitchen || room == room_kitchen_final",
    "Una pequeña y ordenada cocina. Un pastel recién horneado reposa sobre la encimera, llenando el aire quieto de un olor dulce y triste.");
beats += AmbBeat("amb_asghouse1", "room == room_asghouse1",
    "Entras en un hogar casi idéntico al de Toriel (la misma forma, las mismas habitaciones), pero gris, silencioso y largamente abandonado, con todo tal como estaba. Unas escaleras bajan desde el vestíbulo de entrada.");
beats += AmbBeat("amb_asghouse2", "room == room_asghouse2",
    "Un salón que es un calco exacto del de Toriel: un sillón de lectura junto a un hogar apagado, una mesa de comedor puesta para una familia. Pero ahora está en penumbra y vacío, cubierto de un polvo silencioso.");
beats += AmbBeat("amb_asghouse3", "room == room_asghouse3",
    "Un pasillo como el del hogar de Toriel, con un espejo colgado en la pared. Los dormitorios que dan a este pasillo guardan las pertenencias de unos niños que hace mucho que no están.");
beats += AmbBeat("amb_lastruins_corridor", "room == room_lastruins_corridor",
    "Un largo pasillo gris flanqueado por placas. Cada una, al pasar, cuenta otro fragmento de la triste y antigua historia del Subsuelo.");
beats += AmbBeat("amb_sanscorridor", "room == room_sanscorridor",
    "Un vasto salón de luz dorada, con altos pilares proyectando largas sombras y la luz del sol entrando a raudales por grandes ventanales. Cerca de la entrada brilla un punto de guardado, y a lo lejos, en el otro extremo, una figura espera para sopesar el viaje que has hecho.");
beats += AmbBeat("amb_castle_finalshoehorn", "room == room_castle_finalshoehorn",
    "Una tranquila cámara gris con un punto de guardado, con el camino estrechándose hacia la sala del trono que hay delante.");
beats += AmbBeat("amb_castle_coffins2", "room == room_castle_coffins2",
    "Una sala solemne flanqueada por ataúdes del tamaño de un niño, cada uno cuidadosamente elaborado y marcado con un nombre.");
beats += AmbBeat("amb_castle_throneroom", "room == room_castle_throneroom",
    "La sala del trono, alfombrada de flores doradas que brillan bajo la luz que se derrama desde lo alto. Aquí se alzan dos tronos, uno de ellos cubierto y sin usar. Aquí es donde espera el Rey. Cerca brilla un punto de guardado.");
beats += AmbBeat("amb_castle_prebarrier", "room == room_castle_prebarrier",
    "Una solemne cámara gris justo antes de la barrera, con un punto de guardado brillando suavemente aquí.");
beats += AmbBeat("amb_castle_barrier", "room == room_castle_barrier",
    "La barrera en sí: un imponente muro de cegadora luz blanca, la antigua magia que sella todo el Subsuelo, apartándolo del mundo de la superficie.");
beats += AmbBeat("amb_castle_trueexit", "room == room_castle_trueexit",
    "Un pasaje que conduce hacia arriba y adelante, hacia la superficie por fin.");

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
        else external_call(global.nvda_speak, ""No hay descripción para esta zona."");
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
            _say = ""Introducir nombre. Las flechas mueven, Z selecciona una letra, X borra. Escribe un nombre y elige Hecho."";
        else if (naming == 3)
        {
            if (hasname == 1)
            {
                var _o3 = ""Continuar"";
                if (selected3 == 1) { if (truereset == 0) _o3 = ""Reiniciar""; else _o3 = ""Reinicio verdadero""; }
                else if (selected3 == 2) _o3 = ""Ajustes"";
                _say = ""Menú de carga. Izquierda o derecha para Continuar o Reiniciar, abajo para Ajustes y Opciones de accesibilidad. "" + _o3;
            }
            else
            {
                var _b3 = ""Empezar partida"";
                if (selected3 == 1) _b3 = ""Ajustes"";
                _say = ""Menú principal. Arriba o abajo para elegir, Z para confirmar. "" + _b3;
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
                if (selected_col == 0) _say = ""Salir"";
                else if (selected_col == 1) _say = ""Retroceso"";
                else if (selected_col == 2) _say = ""Hecho"";
            }
            else if (selected_row == -2)
            {
                _say = ""Cambiar mayúsculas"";
            }
            else
            {
                var _ch = charmap[selected_row, selected_col];
                if (_ch == """") _say = ""en blanco""; else _say = _ch;
            }
        }
        if (charname != nvda_last_name)
        {
            nvda_last_name = charname;
            if (charname == """") _say = ""nombre vacío""; else _say = ""Nombre "" + charname;
        }
    }
    else if (naming == 2)
    {
        // spec_m / allow / selected2 are only created by the game's own naming==2
        // block, which is skipped on the frame naming flips to 2 (from the grid's
        // ""Hecho"" at line 465 or the Reset path at line 645).  Reading spec_m before
        // the game sets it = fatal ""not set before reading it"".  Wait one frame.
        if (variable_instance_exists(id, ""spec_m"") && variable_instance_exists(id, ""allow"") && variable_instance_exists(id, ""selected2""))
        {
            if (spec_m != nvda_last_specm)
            {
                nvda_last_specm = spec_m;
                nvda_last_sel2 = selected2;
                var _opt = ""Volver atrás"";
                if (allow) { if (selected2 == 1) _opt = ""Sí""; else _opt = ""No""; }
                _say = spec_m + "". "" + _opt;
            }
            else if (selected2 != nvda_last_sel2)
            {
                nvda_last_sel2 = selected2;
                if (allow) { if (selected2 == 1) _say = ""Sí""; else _say = ""No""; }
                else _say = ""Volver atrás"";
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
                if (selected3 == 0) _say = ""Continuar"";
                else if (selected3 == 1) { if (truereset == 0) _say = ""Reiniciar""; else _say = ""Reinicio verdadero""; }
                else if (selected3 == 2) _say = ""Ajustes"";
                else if (selected3 == 3) _say = ""Opciones de accesibilidad"";
            }
            else
            {
                if (selected3 == 0) _say = ""Empezar partida"";
                else if (selected3 == 1) _say = ""Ajustes"";
                else if (selected3 == 2) _say = ""Opciones de accesibilidad"";
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
    external_call(global.nvda_speak, ""Undertale. Pulsa el botón de confirmar, Z o Intro, para empezar."");
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
    var _langname = ""Japonés"";
    if (global.language == ""en"") _langname = ""Inglés"";
    var _lbl = """";
    if (menu == 0) _lbl = ""Salida"";
    else if (menu == 1) _lbl = ""Idioma, "" + _langname;
    else if (menu == 2) _lbl = ""Botón de confirmar"";
    else if (menu == 3) _lbl = ""Botón de cancelar"";
    else if (menu == 4) _lbl = ""Botón de menú"";
    else if (menu == 5) _lbl = ""Restablecer controles"";
    else if (menu == 6) _lbl = ""Borde"";

    var _say = """";
    if (nvda_entered == 0)
    {
        nvda_entered = 1;
        nvda_last_menu = menu;
        nvda_last_lang = global.language;
        nvda_last_engage = menu_engage;
        _say = ""Ajustes. Arriba y abajo para moverte, Z para seleccionar, izquierda y derecha para cambiar un valor. "" + _lbl;
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
            _say = ""Idioma, "" + _langname;
        }
        if (menu_engage != nvda_last_engage)
        {
            nvda_last_engage = menu_engage;
            if (menu_engage == 1 && menu >= 2 && menu <= 4)
                _say = ""Pulsa una tecla para asignarla."";
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
    with (obj_interactable) nvda_kind = ""Objeto"";
    with (obj_wanderparent) nvda_kind = ""Carácter"";
    with (obj_readable) nvda_kind = ""Cartel"";
    with (obj_switch) nvda_kind = ""Interruptor"";
    with (obj_switchbasic) nvda_kind = ""Interruptor"";
    with (obj_readable_switch1) nvda_kind = ""Interruptor"";
    with (obj_redswitch_1) nvda_kind = ""Interruptor"";
    with (obj_savepoint) nvda_kind = ""Punto de guardado"";
    with (obj_pushrock1) nvda_kind = ""Roca"";
    with (obj_floorswitch1) nvda_kind = ""Interruptor de suelo"";
    with (obj_spiketile1) nvda_kind = ""Pinchos"";
    with (obj_spiketile2) nvda_kind = ""Pinchos"";
    with (obj_spikes_room) nvda_kind = ""Pinchos"";
    with (obj_holedown) nvda_kind = ""Agujero"";
    with (obj_superdrophole) nvda_kind = ""Agujero"";
    with (obj_holeup) nvda_kind = ""Salida hacia arriba"";
    with (obj_holeup2) nvda_kind = ""Salida hacia arriba"";
    with (obj_xoxo) { if (image_index == 0) nvda_kind = ""X, písala""; else if (image_index == 1) nvda_kind = ""O, hecho, evita""; else nvda_kind = ""casilla O""; }
    with (obj_xoxocontroller1) nvda_kind = ""Interruptor, pulsa cuando todos estén en O"";
    with (obj_doorparent) nvda_kind = ""Salida"";";

// Build global.nvda_list[] sorted by distance to the player (selection sort; rooms are small).
string buildList = @"
    var _list = ds_list_create();
    with (obj_interactable) ds_list_add(_list, id);
    // doors, but NOT the ice-slide trigger tiles (obj_iceevent/up/right are obj_doorparent
    // children; a slide room has 100+ of them -> they flooded the scanner as fake ""salidas"").
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
    if (_dy < -16) _dir = ""arriba""; else if (_dy > 16) _dir = ""abajo"";
    if (_dx < -16) { if (_dir != """") _dir += "" y ""; _dir += ""izquierda""; } else if (_dx > 16) { if (_dir != """") _dir += "" y ""; _dir += ""derecha""; }
    if (_dir == """") _dir = ""aquí"";
    var _kind = ""Objeto"";
    if (variable_instance_exists(_sel, ""nvda_kind"")) _kind = _sel.nvda_kind;
    var _nm = object_get_name(_sel.object_index);
    if (string_length(_nm) > 4 && string_copy(_nm, 1, 4) == ""obj_"") _nm = string_copy(_nm, 5, string_length(_nm) - 4);
    _nm = string_replace_all(_nm, ""_"", "" "");
    var _state = """";
    if (_kind == ""Interruptor"" && variable_instance_exists(_sel, ""activado"")) { if (_sel.on) _state = "", on""; else _state = "", off""; }
    var _out = _kind + "", "" + _nm + _state;
    if (_dir == ""aquí"") _out += "", aquí""; else _out += "", "" + _dir + "", "" + string(_steps) + "" pasos"";";

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
            external_call(global.nvda_speak, ""Aquí no hay nada con lo que interactuar."");
        }
        else
        {
            if (keyboard_check_pressed(ord(""R""))) global.nvda_idx -= 1; else global.nvda_idx += 1;
            if (global.nvda_idx >= global.nvda_listn) global.nvda_idx = 0;
            if (global.nvda_idx < 0) global.nvda_idx = global.nvda_listn - 1;
            global.nvda_sel = global.nvda_list[global.nvda_idx];
            var _sel = global.nvda_sel;" + announce + @"
            external_call(global.nvda_speak, _out + "". "" + string(global.nvda_idx + 1) + "" de "" + string(global.nvda_listn));
            nvda_lastdir = _dir;
        }
    }

    // E: summary - count + nearest interactable.
    if (keyboard_check_pressed(ord(""E"")))
    {" + tag + buildList + @"
        if (global.nvda_listn <= 0)
        {
            external_call(global.nvda_speak, ""No hay nada interactivo cerca."");
        }
        else
        {
            var _sel = global.nvda_list[0];" + announce + @"
            external_call(global.nvda_speak, string(global.nvda_listn) + "" objetos interactivos. El más cercano: "" + _out);
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
        else external_call(global.nvda_speak, ""Sin objetivo. Pulsa tabulador para elegir."");
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
                    if (_dy < -12) _dr = ""arriba""; else if (_dy > 12) _dr = ""abajo"";
                    if (_dx < -12) { if (_dr != """") _dr += "" ""; _dr += ""izquierda""; } else if (_dx > 12) { if (_dr != """") _dr += "" ""; _dr += ""derecha""; }
                    if (_dr == """") _dr = ""activado"";
                    _msg += object_get_name(object_index) + "" "" + _dr + "". "";
                    _cnt += 1;
                }
            }
        }
        if (_cnt == 0) _msg = ""No hay nada al alcance."";
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
            if (_dy < -16) _dir = ""arriba""; else if (_dy > 16) _dir = ""abajo"";
            if (_dx < -16) { if (_dir != """") _dir += "" y ""; _dir += ""izquierda""; } else if (_dx > 16) { if (_dir != """") _dir += "" y ""; _dir += ""derecha""; }
            if (_dir == """") _dir = ""aquí"";
            if (_dir != nvda_lastdir)
            {
                nvda_lastdir = _dir;
                if (_dir == ""aquí"") external_call(global.nvda_speak, ""Has llegado"");
                else external_call(global.nvda_speak, _dir + "", "" + string(_steps) + "" pasos"");
            }
        }

        var _tried = (obj_time.up || obj_time.down || obj_time.left || obj_time.right);
        if (_tried && x == xprevious && y == yprevious && !_walking)
        {
            if (nvda_blocktimer <= 0)
            {
                external_call(global.nvda_speak, ""Bloqueado"");
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
                    if (_hdy < -12) _hdir = ""arriba""; else if (_hdy > 12) _hdir = ""abajo"";
                    if (_hdx < -12) { if (_hdir != """") _hdir += "" ""; _hdir += ""izquierda""; } else if (_hdx > 12) { if (_hdir != """") _hdir += "" ""; _hdir += ""derecha""; }
                    if (_hdir == """") _hdir = ""aquí"";
                    external_call(global.nvda_speak, ""Agujero "" + _hdir);
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
        var _b = ""Luchar"";
        if (global.bmenucoord[0] == 1) _b = ""Actuar""; else if (global.bmenucoord[0] == 2) _b = ""Objeto""; else if (global.bmenucoord[0] == 3) _b = ""Piedad"";
        external_call(global.nvda_speak, ""Tu turno. PV "" + string(global.hp) + "" de "" + string(global.maxhp) + "". "" + _b);
        nvda_pc0 = global.bmenucoord[0];
    }
    nvda_pinmenu = _inmenu;

    // Submenu change: reset cursor trackers + set a one-shot prefix for the next announce.
    if (global.bmenuno != nvda_pmenu)
    {
        nvda_pmenu = global.bmenuno;
        nvda_pc1 = -99; nvda_pc2 = -99; nvda_pc3 = -99; nvda_pc4 = -99;
        nvda_prefix = """";
        if (global.bmenuno == 1 || global.bmenuno == 11) nvda_prefix = ""Luchar. "";
        else if (global.bmenuno == 2) nvda_prefix = ""Actuar activado. "";
        else if (global.bmenuno == 10) nvda_prefix = ""Actuar. "";
        else if (global.bmenuno >= 3 && global.bmenuno < 4) nvda_prefix = ""Objeto. "";
        else if (global.bmenuno == 4) nvda_prefix = ""Piedad. "";
    }

    // Buttons (top row).
    if (global.bmenuno == 0 && _inmenu)
    {
        if (global.bmenucoord[0] != nvda_pc0)
        {
            nvda_pc0 = global.bmenucoord[0];
            var _b = ""Luchar"";
            if (nvda_pc0 == 1) _b = ""Actuar""; else if (nvda_pc0 == 2) _b = ""Objeto""; else if (nvda_pc0 == 3) _b = ""Piedad"";
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
            if (global.monstermaxhp[nvda_pc1] > 0) _hp = "", PV "" + string(global.monsterhp[nvda_pc1]) + "" de "" + string(global.monstermaxhp[nvda_pc1]);" + Spareable("nvda_pc1") + @"
            var _sps = """";
            if (_sp) _sps = "", se puede perdonar"";
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
                var _p = string_pos(""&"", string_replace_all(_s, ""#"", ""&""));
                if (_p == 0) { _ok = 0; break; }
                _s = string_copy(_s, _p + 1, string_length(_s) - _p);
                _ri += 1;
            }
            var _lbl = """";
            if (_ok)
            {
                var _pe = string_pos(""&"", string_replace_all(_s, ""#"", ""&""));
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
            if (_lbl == """") _lbl = ""Opción "" + string(nvda_pc2 + 1);
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
            var _m = ""Perdonar"";
            if (nvda_pc4 == 1) _m = ""Huir"";
            else
            {
                var _anysp = 0;
                for (var _k = 0; _k < 3; _k += 1)
                {" + Spareable("_k") + @"
                    if (_sp) _anysp = 1;
                }
                if (_anysp) _m = ""Perdonar, ya puedes perdonar"";
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
            if (_lc == ""froggit"" || _lc == ""final froggit"") _md = ""Un monstruo pequeño y rechoncho con aspecto de rana, pálido y blando, que se queda pestañeando ante ti con grandes ojos redondos y una boca ancha y apacible."";
            else if (_lc == ""whimsun"") _md = ""Un monstruo alado, diminuto y tímido, parecido a una pequeña polilla. Tiene una carita cabizbaja y tiembla con nerviosismo, como si lamentara siquiera estar luchando."";
            else if (_lc == ""moldsmal"") _md = ""Un montículo tembloroso de moho gelatinoso de color verde pálido. Se menea con suavidad en el sitio y no parece tener la menor idea de cómo hacer daño a nadie."";
            else if (_lc == ""migosp"") _md = ""Un pequeño monstruo insecto de color oscuro, parecido a un escarabajo, con ojos redondos y bracitos que agita. Lejos de la multitud es tímido e inofensivo."";
            else if (_lc == ""vegetoid"") _md = ""Un monstruo vegetal, grande y amistoso, que emerge del suelo con forma de zanahoria gigante y una amplia cara sonriente llena de dientes cuadrados."";
            else if (_lc == ""loox"") _md = ""Un monstruo pequeño y redondo cubierto de pelaje suave, con un gran ojo en el centro de la cara. Parece malhumorado, pero en realidad solo no quiere que se metan con él."";
            else if (_lc == ""napstablook"") _md = ""Un pequeño y tímido fantasma blanco de cara caída y ojos somnolientos entornados. Flota en silencio y siempre parece a punto de llorar."";
            else if (_lc == ""dummy"") _md = ""Un muñeco de entrenamiento de tela sobre un soporte de madera, con una cara sencilla cosida. Permanece ahí en silencio, esperando."";
            else if (_lc == ""toriel"") _md = ""El monstruo alto y amable con aspecto de cabra, con su larga túnica morada, interponiéndose entre tú y la puerta con una mirada dolida y protectora."";
            else if (_lc == ""snowdrake"") _md = ""Un pequeño monstruo pájaro de color azul, hecho para el frío, con una cresta de plumas heladas. Un adolescente que se esfuerza al máximo por soltar un buen chiste."";
            else if (_lc == ""ice cap"" || _lc == ""icecap"") _md = ""Un pequeño monstruo peludo casi oculto bajo un enorme gorro puntiagudo de hielo del que está sumamente orgulloso. Solo sus ojos asoman por debajo."";
            else if (_lc == ""gyftrot"") _md = ""Un monstruo peludo y fatigado con aspecto de ciervo, cuya cornamenta han cubierto de trastos unos niños bromistas. Solo quiere que se los quiten."";
            else if (_lc == ""doggo"") _md = ""Un monstruo perro sentado en una garita de madera, empuñando dos dagas brillantes y con los ojos yendo de un lado a otro. Solo puede ver lo que se mueve."";
            else if (_lc == ""lesser dog"") _md = ""Un pequeño monstruo perro blanco con una armadura, espada en mano, con la lengua colgando alegremente y un cuello que se estira más cuanto más se emociona."";
            else if (_lc == ""greater dog"") _md = ""Un pequeño perro blanco casi perdido dentro de una enorme armadura, moviendo la cola con entusiasmo. Preferiría con creces jugar antes que luchar."";
            else if (_lc == ""dogamy"") _md = ""Uno de un matrimonio de monstruos perro con túnicas negras con capucha, blandiendo un hacha grande y olfateando el aire en busca de tu olor."";
            else if (_lc == ""dogaressa"") _md = ""Una de un matrimonio de monstruos perro con túnicas negras con capucha, caminando junto a su marido con un hacha en la pata."";
            else if (_lc == ""papyrus"") _md = ""El esqueleto muy alto y larguirucho con su disfraz blanco casero y su larga bufanda roja, adoptando una pose heroica mientras te hace frente."";
            else if (_lc == ""chilldrake"") _md = ""Un descarado monstruo pájaro azul, primo del Snowdrake, con una cresta de plumas heladas y una actitud aún más fría."";
            // ---- Waterfall ----
            else if (_lc == ""mad dummy"") _md = ""Un muñeco de tela que flota en el aire, temblando de rabia mientras un fantasma furioso proyecta su voz a través de él. Su cara cosida está retorcida en una mueca furiosa."";
            else if (_lc == ""aaron"") _md = ""Un musculoso monstruo con aspecto de caballito de mar y una sonrisa segura de sí misma, que no para de marcar sus enormes brazos. Le interesa mucho más presumir que luchar."";
            else if (_lc == ""woshua"") _md = ""Un pequeño monstruo azul con forma parecida a un cangrejo y un cepillo de fregar por cresta. Está obsesionado con la limpieza y solo quiere que todo esté ordenado."";
            else if (_lc == ""moldbygg"") _md = ""Una columna alta y temblorosa de moho verde pálido, como un primo más alto del Moldsmal. Se balancea con suavidad y le encantan los buenos abrazos."";
            else if (_lc == ""temmie"") _md = ""Una pequeña y extraña criatura dibujada con un estilo tosco y garabateado: una cara felina de grandes orejas y ojos muy abiertos sobre un pequeño cuerpo peludo de color marrón."";
            else if (_lc == ""shyren"") _md = ""Un tímido monstruo con aspecto de sirena que oculta su rostro, tarareando una melodía suave y temblorosa. Es demasiado vergonzosa como para mirarte directamente."";
            else if (_lc == ""glyde"") _md = ""Un monstruo esponjoso con aspecto de nube y una cazadora bómber, que flota con una chulería torpe y demasiado forzada. Un raro vagabundo que no acaba de tener claro que encaje."";
            else if (_lc == ""jerry"") _md = ""Un pequeño monstruo gris y grumoso con una expresión permanentemente engreída y ajena a todo. Francamente, a nadie le gusta tener cerca a Jerry."";
            else if (_lc == ""undyne"") _md = ""Una alta y poderosa guerrera pez con reluciente armadura, la melena roja al viento y un ojo ardiendo tras un parche mientras invoca lanzas brillantes. La jefa de la Guardia Real, y no piensa dar su brazo a torcer."";
            // ---- Hotland and the CORE ----
            else if (_lc == ""vulkin"") _md = ""Un pequeño y redondo monstruo volcán que brilla cálido en su cráter, con bracitos rechonchos y una cara radiante y entusiasta. Solo quiere ayudar, aunque su ayuda queme un poco."";
            else if (_lc == ""tsunderplane"") _md = ""Un monstruo avión de combate con una cara ruborizada y vergonzosa en el morro. Insiste en que desde luego no vuela tan cerca de ti a propósito."";
            else if (_lc == ""pyrope"") _md = ""Un monstruo redondo y llameante, como una brasa viviente con una amplia sonrisa, que irradia calor y lo quiere todo más caliente."";
            else if (_lc == ""madjick"") _md = ""Un alto monstruo mago con capa y un sombrero ancho y puntiagudo, con dos orbes mágicos brillantes girando a su alrededor. Solo se le ven los ojos centelleantes bajo el ala del sombrero."";
            else if (_lc == ""knight knight"") _md = ""Un monstruo enorme y muy acorazado, como un caballero descomunal, con un yelmo en forma de luna creciente y una maza descomunal. Lento, somnoliento e inmensamente fuerte."";
            else if (_lc == ""final froggit"") _md = ""Un Froggit más duro y veterano de las profundidades del Subsuelo. El mismo cuerpo de rana, blando y pálido, y los mismos grandes ojos, pero con una mirada más sabia y decidida."";
            else if (_lc == ""whimsalot"") _md = ""Un Whimsun convertido en un diminuto caballero acorazado, con alitas, un casco y una lanza, tratando con valentía de parecer feroz."";
            else if (_lc == ""astigmatism"") _md = ""Un monstruo amarillo y redondo con un gran ojo y una boca ancha y dentuda, con la mirada fija y severa. Insiste mucho en que prestes atención."";
            else if (_lc == ""migospel"") _md = ""Un Migosp que ha encontrado su confianza: un pequeño monstruo escarabajo que agita los brazos con alegría al ritmo de una música que solo él oye."";
            else if (_lc == ""so sorry"") _md = ""Un pequeño y azorado monstruo con aspecto de dragón, de traje formal, que se enreda en sus propias disculpas. De verdad que no pretendía estar aquí y lo siente muchísimo."";
            else if (_lc == ""muffet"") _md = ""Un elegante monstruo araña de color morado, con cinco ojos y cinco brazos, con un atuendo de volantes y una taza de té sostenida con delicadeza en una mano mientras sus arañas mascota corretean a su alrededor."";
            else if (_lc == ""mettaton"") _md = ""El robot con forma de caja metálica rectangular sobre una única rueda, con diales y una pantalla en la parte frontal y brazos delgados a cada lado, presentando todo esto como un deslumbrante programa de televisión."";
            else if (_lc == ""mettaton ex"" || _lc == ""mettatonex"") _md = ""Un robot fabuloso con una elegante forma humanoide, todo en negro y rosa, sostenido sobre una única pierna con rueda en una pose dramática bajo las luces del escenario."";
            else if (_lc == ""mettaton neo"") _md = ""Un robot descomunal en forma de combate, erizado de cañones y blindaje, con las alas desplegadas, hecho para parecer absolutamente imparable."";
            // ---- Bosses and the amalgamates ----
            else if (_lc == ""asgore"") _md = ""Un enorme rey cabra con armadura morada sobre una capa real, inmenso y de anchos hombros, con largos cuernos curvos y una barba dorada. Alza un gran tridente, y sus ojos están llenos de pesar."";
            else if (_lc == ""sans"") _md = ""El esqueleto bajito con la chaqueta azul con capucha, las manos en los bolsillos, sonriendo como siempre, con un ojo parpadeando con una extraña luz azul. No parece tomarse esto en serio, y eso es lo más peligroso de él."";
            else if (_lc == ""endogeny"") _md = ""Una amalgama grande e inquietante de muchos monstruos perro fundidos entre sí, una masa blanca y goteante con una única cara canina y demasiadas extremidades. Se abalanza hacia ti con ganas de jugar."";
            else if (_lc == ""reaper bird"") _md = ""Una amalgama alta y espeluznante de monstruos pájaro, de alas oscuras, cuello largo y un rostro hueco de mirada fija. Se desplaza de forma antinatural, varios seres a la vez."";
            else if (_lc == ""lemon bread"") _md = ""Una amalgama pálida de partes de monstruo fundidas en un cuerpo largo y serpenteante con aletas y demasiadas bocas, que se contonea con una extraña elegancia."";
            else if (_lc == ""memoryhead"") _md = ""Una amalgama flotante y a medio formar, como un rostro blanco que se derrite sobre un tallo, que murmura suavemente y se extiende hacia ti como si te conociera."";
            else if (_lc == ""snowdrake's mother"" || _lc == ""snowman"" || string_pos(""snowdrake's"", _lc) > 0) _md = ""Un monstruo pájaro grande y apacible, madre del pequeño Snowdrake, de plumas pálidas y suaves. Arrastra una tristeza callada y cansada."";
            // ---- Hard-mode Ruins variants ----
            else if (_lc == ""moldessa"") _md = ""Un primo del Moldsmal en modo difícil: un montículo tembloroso de moho con algo más de carácter."";
            else if (_lc == ""parsnik"") _md = ""Un primo del Vegetoid en modo difícil: un gran monstruo tubérculo con una sonrisa más mordaz."";
            if (_md == """") _md = ""Aún no hay descripción escrita para "" + _mn + ""."";
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
    external_call(global.nvda_speak, ""Ataca. Pulsa Z en el pitido más agudo."");
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
            external_call(global.nvda_speak, ""¡Golpe! "" + string(global.damage) + "" de daño."");
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
    var _msg = ""Tienes "" + string(global.gold) + "" de oro. "" + string(itemfree) + "" de 8 espacios de inventario libres."";
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
        external_call(global.nvda_speak, ""Modo asistido. No puedes ser derrotado."");
    else if (global.nvda_mode == 1)
        external_call(global.nvda_speak, ""Modo lento. Los combates van a media velocidad. Puedes ser derrotado."");
    else
        external_call(global.nvda_speak, ""Modo normal. Velocidad completa. Puedes ser derrotado."");
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
        var _h = ""centro"";
        if (_hf < 0.33) _h = ""izquierda""; else if (_hf > 0.66) _h = ""derecha"";
        var _v = ""en medio"";
        if (_vf < 0.33) _v = ""arriba""; else if (_vf > 0.66) _v = ""abajo"";
        external_call(global.nvda_speak, ""Corazón "" + _v + "" "" + _h);
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
// _pre is a leading phrase (e.g. ""Modo salto. "" on entry, """" on cycle).
string announce = @"
var _door = global.nvda_dlid[nvda_skipidx];
var _ddx = _door.x - x;
var _ddy = _door.y - y;
var _dir = ""derecha"";
if (abs(_ddx) >= abs(_ddy)) { if (_ddx < 0) _dir = ""izquierda""; else _dir = ""derecha""; }
else { if (_ddy < 0) _dir = ""arriba""; else _dir = ""abajo""; }
var _steps = round(point_distance(x, y, _door.x, _door.y) / 20);
external_call(global.nvda_speak, _pre + ""Salida "" + string(nvda_skipidx + 1) + "" de "" + string(global.nvda_doorn) + "", "" + _dir + "", "" + string(_steps) + "" pasos."");";

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
    external_call(global.nvda_speak, ""Salto cancelado."");
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
            external_call(global.nvda_speak, ""No hay salidas en esta sala."");
        }
        else
        {
            nvda_skipmode = 1;
            nvda_skipidx = 0;
            var _pre = ""Modo salto. "" + string(global.nvda_doorn) + "" salidas. "";" + announce + @"
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
    external_call(global.nvda_speak, ""Cruzando la salida "" + string(nvda_skipidx + 1) + ""."");
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
            var _nm = ""Objeto""; if (_o == 1) _nm = ""Estadísticas""; if (_o == 2) _nm = ""Móvil"";
            _say = _nm;
            if (_enter) _say = ""Menu. "" + _say;
        }
        else if (_mn == 1 || _mn == 6)
        {
            var _it = global.itemname[global.menucoord[_mn]];
            if (_it == """") _it = ""Vacío"";
            _say = _it;
            if (_enter) _say = ""Objetos. "" + _say;
        }
        else if (_mn == 5)
        {
            var _a = global.menucoord[5];
            var _an = ""Usar""; if (_a == 1) _an = ""Información""; if (_a == 2) _an = ""Tirar"";
            _say = _an;
            if (_enter) _say = ""Elige una acción. "" + _say;
        }
        else if (_mn == 7)
        {
            var _it = global.itemname[global.menucoord[7]];
            if (_it == """") _it = ""Vacío"";
            _say = _it;
            if (_enter) _say = ""Caja. "" + _say;
        }
        else if (_mn == 3)
        {
            var _ph = global.phonename[global.menucoord[3]];
            if (_ph == """") _ph = ""Vacío"";
            _say = _ph;
            if (_enter) _say = ""Móvil. "" + _say;
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
                _say = ""Estadísticas. "" + _nm + "". LOVE "" + string(_lv) +
                       "". PV "" + string(_hp) + "" de "" + string(_mhp) +
                       "". Ataque "" + string(_at) + "" plus "" + string(_ws) +
                       "". Defensa "" + string(_df) + "" plus "" + string(_ad) +
                       "". Oro "" + string(_gd) + "". Exp "" + string(_xp) +
                       "". Siguiente "" + string(_nl) + ""."";
            }
        }
        else if (_mn == 4)
        {
            var _s = global.menucoord[4];
            if (_s == 2)
                _say = ""Guardado."";
            else
            {
                var _opt = ""Guardar""; if (_s == 1) _opt = ""Volver"";
                _say = _opt;
                if (_enter)
                    _say = ""Punto de guardado. "" + name + "". LOVE "" + string(love) + "". "" +
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
    _qt = string_replace_all(_qt, ""&"", "" ""); _qt = string_replace_all(_qt, ""#"", "" "");

    // Announce the question + options once per question.
    if (q >= 1 && phase >= 1 && phase <= 2 && nvda_qann != q)
    {
        nvda_qann = q;
        nvda_pend = -1;
        nvda_subm = 0;
        var _msg = ""Pregunta. "" + _qt + "". 1 A: "" + _oa1 + "". 2 B: "" + _oa2 +
                   "". 3 C: "" + _oa3 + "". 4 D: "" + _oa4 + ""."";
        if (q != 7 && correct >= 0 && correct <= 3)
        {
            var _hl = ""A""; if (correct == 1) _hl = ""B""; if (correct == 2) _hl = ""C""; if (correct == 3) _hl = ""D"";
            _msg += "" pistas de Alphys "" + _hl + ""."";
        }
        _msg += "" Pulsa del 1 al 4 y luego Z para confirmar."";
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
            external_call(global.nvda_speak, _lt + "". "" + _tx + "". Pulsa Z para confirmar."");
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
            external_call(global.nvda_speak, ""Fijado en "" + _lt2 + ""."");
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
        external_call(global.nvda_speak, ""Puzle de disparos. Este es visual. Para saltarlo, pulsa P y luego O."");
    }
    if (win > 0) nvda_sgann = 0;   // reset for the next puzzle room

    // P arms, O confirms -> force the win so the game's own win sequence finishes it.
    if (win == 0 && keyboard_check_pressed(ord(""P"")))
    {
        nvda_sgarm = 1;
        external_call(global.nvda_speak, ""¿Saltar el puzle? Pulsa O para confirmar."");
    }
    if (win == 0 && nvda_sgarm == 1 && keyboard_check_pressed(ord(""O"")))
    {
        nvda_sgarm = 0;
        active = 1;
        win = 1;
        external_call(global.nvda_speak, ""Puzle saltado. Resolviendo."");
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
            if (_o == 0) _say = ""Comprar""; else if (_o == 1) _say = ""Vender""; else if (_o == 2) _say = ""Hablar""; else if (_o == 3) _say = ""Salida"";
            if (murder == 1) { if (_o == 0) _say = ""Coger""; else if (_o == 1) _say = ""Robar""; else if (_o == 2) _say = ""Leer""; }
            if (_enter) _say = ""Tienda. "" + _say;
        }
        else if (_m == 1)
        {
            var _c = menuc[1];
            if (_c >= 0 && _c <= 3)
                _say = scr_gettext(""item_name_"" + string(item[_c])) + "", "" + string(itemcost[_c]) + "" gold"";
            else
                _say = ""Salida"";
            if (_enter) _say = ""Comprar. "" + _say;
        }
        else if (_m == 2)
        {
            if (menuc[2] == 0) _say = ""Sí, comprar por "" + string(itemcost[menuc[1]]) + "" gold"";
            else _say = ""No"";
            if (_enter) _say = ""Confirmar. "" + _say;
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
            else _say = ""Salida"";
            if (_enter) _say = ""Hablar. "" + _say;
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
            external_call(global.nvda_speak, ""Las seis almas están libres. Flowey está débil ahora. Pulsa Z para atacarlo con el botón de luchar."");
        else
            external_call(global.nvda_speak, ""Alma "" + string(global.soul_rescue) + "" de 6 liberadas. Tus ataques golpean más fuerte ahora."");
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
        if (_m == 1) external_call(global.nvda_speak, ""Ronda de almas. Pulsa Z para liberar esta alma. Sigue el pitido si quieres, pero Z funciona desde cualquier sitio."");
        else if (_m == 2) external_call(global.nvda_speak, ""Botón de luchar activo. Pulsa Z para atacar a Flowey."");
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
            external_call(global.nvda_speak, ""Liberando el alma."");
        }
        else if (_fbt)
        {
            with (obj_flowey_fightbt) event_user(4);
            external_call(global.nvda_speak, ""Atacando a Flowey."");
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
        external_call(global.nvda_speak, ""Liberando el alma automáticamente."");
    }

    // O = diagnostic: read the current scene out loud.
    if (keyboard_check_pressed(ord(""O"")))
    {
        var _sr = 0; if (variable_global_exists(""soul_rescue"")) _sr = global.soul_rescue;
        var _hp = 0; if (variable_global_exists(""my_hp"")) _hp = global.my_hp;
        var _w = ""Diagnóstico. "";
        if (_soul) _w += ""Ronda de almas activa. ""; else _w += ""No hay ronda de almas. "";
        if (_fbt) _w += ""Botón de luchar presente. ""; else _w += ""No hay botón de luchar. "";
        _w += string(_sr) + "" de 6 almas liberadas. PV "" + string(_hp) + ""."";
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
                else if (c == ""&"" || c == ""#"" || c == ""/"" || c == ""%"" || c == ""*"")
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
                    if (ec == ""&"" || ec == ""#"" || ci > clen)
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
                external_call(global.nvda_speak, ""Elección. "" + pick + "", or "" + alt + "". Pulsa izquierda o derecha y luego Z. En "" + pick);
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
        external_call(global.nvda_speak, ""Escudo. Bloquea cada lanza con la flecha hacia ella. Pitido: oído izquierdo o derecho es izquierda o derecha, tono agudo es arriba, tono grave es abajo."");
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
                    else if (bc == ""&"" || bc == ""#"" || bc == ""/"" || bc == ""%"" || bc == ""*"")
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
                        if (bec == ""&"" || bec == ""#"" || bci > bclen)
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
                external_call(global.nvda_speak, ""Elige. "" + bpick + "", or "" + balt + "". Pulsa izquierda o derecha y luego Z. En "" + bpick);
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
        var _intro = ""Elige. Mueve tu corazón hasta una casilla y pulsa Z. Oirás cada casilla al llegar a ella."";
        if (type == 0) _intro = ""Puedes matarlo o perdonarlo. Matar está a la izquierda. Perdonar está a la derecha. Mueve tu corazón hasta una casilla y pulsa Z."";
        external_call(global.nvda_speak, _intro);
    }
}";

string stepGml = @"
{" + nvdaBridge + @"
    if (!variable_instance_exists(id, ""nvda_bt_set""))
    {
        nvda_bt_set = 1;
        nvda_laston = 0;
        nvda_lbl = ""Caja"";
        if (type == 0) nvda_lbl = ""Matar"";
        else if (type == 1) nvda_lbl = ""Perdonar"";
        else if (type == 2) nvda_lbl = ""Luchar"";
        else if (type == 3) nvda_lbl = ""Perdonar"";
    }
    if (on > 0 && nvda_laston <= 0)
    {
        external_call(global.nvda_speak, nvda_lbl + "". Pulsa Z."");
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
        if (_pct <= 0) external_call(global.nvda_speak, ""Música desactivada"");
        else external_call(global.nvda_speak, ""Música "" + string(_pct) + "" por ciento"");
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
        _pre = ""Menú de accesibilidad. Arriba y abajo para moverte, izquierda y derecha para cambiar. Pulsa K o cancelar para cerrar. "";
    }
    if (global.nvda_menu_open == 0 && global.nvda_menu_wasopen == 1)
    {
        if (instance_exists(obj_mainchara) && !instance_exists(obj_intromenu) && variable_global_exists(""interact"")) global.interact = 0;
        external_call(global.nvda_speak, ""Menú de accesibilidad cerrado."");
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
        if (_s2 == 0) { _lbl = ""Lectura de pantalla""; if (global.nvda_opt_speech == 1) _val = ""activado""; else _val = ""desactivado""; }
        else if (_s2 == 1) { _lbl = ""Escaneo de objetos y guía""; if (global.nvda_opt_scan == 1) _val = ""activado""; else _val = ""desactivado""; }
        else if (_s2 == 2) { _lbl = ""Saltar puzles""; if (global.nvda_opt_skip == 1) _val = ""activado""; else _val = ""desactivado""; }
        else if (_s2 == 3) { _lbl = ""Sonidos de navegación""; if (global.nvda_opt_navsfx == 1) _val = ""activado""; else _val = ""desactivado""; }
        else if (_s2 == 4) { _lbl = ""Indicaciones de combate""; if (global.nvda_opt_combat == 1) _val = ""activado""; else _val = ""desactivado""; }
        else if (_s2 == 5)
        {
            _lbl = ""Modo asistencia"";
            if (global.nvda_mode == 0) _val = ""Asistido, no puedes ser derrotado"";
            else if (global.nvda_mode == 1) _val = ""Lento, los combates van a media velocidad"";
            else _val = ""Normal, puedes ser derrotado"";
        }
        else if (_s2 == 6) { _lbl = ""Volumen de música""; _val = string(round(global.nvda_musicvol * 100)) + "" por ciento""; }
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
        _pre = ""Menú de accesibilidad. Arriba y abajo para moverte, izquierda y derecha para cambiar. Pulsa K o cancelar para cerrar. "";
    }
    // edge: just closed -> unfreeze the player (if any) and confirm
    if (global.nvda_menu_open == 0 && global.nvda_menu_wasopen == 1)
    {
        if (instance_exists(obj_mainchara) && !instance_exists(obj_intromenu) && variable_global_exists(""interact"")) global.interact = 0;
        external_call(global.nvda_speak, ""Menú de accesibilidad cerrado."");
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
        if (_s2 == 0) { _lbl = ""Lectura de pantalla""; if (global.nvda_opt_speech == 1) _val = ""activado""; else _val = ""desactivado""; }
        else if (_s2 == 1) { _lbl = ""Escaneo de objetos y guía""; if (global.nvda_opt_scan == 1) _val = ""activado""; else _val = ""desactivado""; }
        else if (_s2 == 2) { _lbl = ""Saltar puzles""; if (global.nvda_opt_skip == 1) _val = ""activado""; else _val = ""desactivado""; }
        else if (_s2 == 3) { _lbl = ""Sonidos de navegación""; if (global.nvda_opt_navsfx == 1) _val = ""activado""; else _val = ""desactivado""; }
        else if (_s2 == 4) { _lbl = ""Indicaciones de combate""; if (global.nvda_opt_combat == 1) _val = ""activado""; else _val = ""desactivado""; }
        else if (_s2 == 5)
        {
            _lbl = ""Modo asistencia"";
            if (global.nvda_mode == 0) _val = ""Asistido, no puedes ser derrotado"";
            else if (global.nvda_mode == 1) _val = ""Lento, los combates van a media velocidad"";
            else _val = ""Normal, puedes ser derrotado"";
        }
        else if (_s2 == 6) { _lbl = ""Volumen de música""; _val = string(round(global.nvda_musicvol * 100)) + "" por ciento""; }
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
            external_call(global.nvda_speak, ""Menú de accesibilidad cerrado."");
            exit;
        }
        // edge: just opened -> intro + first option
        if (global.nvda_menu_wasopen == 0)
        {
            global.nvda_menu_sel = 0;
            _announce = 1;
            _pre = ""Menú de accesibilidad. Arriba y abajo para moverte, izquierda y derecha para cambiar. Pulsa X o K para cerrar. "";
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
            if (_s2 == 0) { _lbl = ""Lectura de pantalla""; if (global.nvda_opt_speech == 1) _val = ""activado""; else _val = ""desactivado""; }
            else if (_s2 == 1) { _lbl = ""Escaneo de objetos y guía""; if (global.nvda_opt_scan == 1) _val = ""activado""; else _val = ""desactivado""; }
            else if (_s2 == 2) { _lbl = ""Saltar puzles""; if (global.nvda_opt_skip == 1) _val = ""activado""; else _val = ""desactivado""; }
            else if (_s2 == 3) { _lbl = ""Sonidos de navegación""; if (global.nvda_opt_navsfx == 1) _val = ""activado""; else _val = ""desactivado""; }
            else if (_s2 == 4) { _lbl = ""Indicaciones de combate""; if (global.nvda_opt_combat == 1) _val = ""activado""; else _val = ""desactivado""; }
            else if (_s2 == 5)
            {
                _lbl = ""Modo asistencia"";
                if (global.nvda_mode == 0) _val = ""Asistido, no puedes ser derrotado"";
                else if (global.nvda_mode == 1) _val = ""Lento, los combates van a media velocidad"";
                else _val = ""Normal, puedes ser derrotado"";
            }
            else if (_s2 == 6) { _lbl = ""Volumen de música""; _val = string(round(global.nvda_musicvol * 100)) + "" por ciento""; }
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
            external_call(global.nvda_speak, ""Detenido."");
        }
        else if (!(variable_global_exists(""nvda_sel"") && global.nvda_sel != noone && instance_exists(global.nvda_sel)))
        {
            external_call(global.nvda_speak, ""No hay objetivo seleccionado. Pulsa T para elegir uno."");
        }
        else if (point_distance(obj_mainchara.x, obj_mainchara.y, global.nvda_sel.x, global.nvda_sel.y) < 14)
        {
            external_call(global.nvda_speak, ""Ya estás ahí."");
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
            external_call(global.nvda_speak, ""Caminando."");
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
            external_call(global.nvda_speak, ""Detenido."");
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
                external_call(global.nvda_speak, ""Has llegado."");
            }
            else if (global.nvda_walk_nogain > 150)
            {
                if (global.nvda_walk_grid >= 0) mp_grid_destroy(global.nvda_walk_grid);
                if (global.nvda_walk_path >= 0) path_delete(global.nvda_walk_path);
                global.nvda_walk_grid = -1; global.nvda_walk_path = -1;
                global.nvda_walk_active = 0; global.nvda_walk_node = 0;
                up = 0; down = 0; left = 0; right = 0;
                if (_distT < (_hw + _hh + 12)) external_call(global.nvda_speak, ""Has llegado."");
                else external_call(global.nvda_speak, ""Camino bloqueado. Deteniéndose."");
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
                            external_call(global.nvda_speak, ""No se ha encontrado ningún camino."");
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
                                external_call(global.nvda_speak, ""Has llegado."");
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
            external_call(global.nvda_speak, ""Alma azul. Pulsa arriba para saltar. El mod ajusta la altura del salto por ti, así que solo tienes que cronometrar cada salto con los pitidos. Un pitido agudo es un hueso pequeño, un pitido grave es un hueso grande, y un pulso grave y lento significa un hueso en el techo, así que quédate abajo."");
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
