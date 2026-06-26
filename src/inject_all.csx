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

// Sanitize <srcExpr> (strip Undertale control codes, exactly mirroring the
// game's own parser in obj_base_writer Alarm_0) and speak it via NVDA.
//   ^N        = pause            -> skip 2 (^ + 1 arg)
//   \S? \E? \F? \M? \T? \*?      -> skip 3 (\ + cmd + 1 arg)
//   \z \R \G \W ... (other \X)   -> skip 2 (\ + cmd)
//   &         = newline          -> space
//   / % *     = markers/bullet   -> drop
string SpeakCore(string srcExpr) => @"
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
        external_call(global.nvda_speak, _out);
}";

string bridgeInit = @"
if (!variable_global_exists(""nvda_ready""))
{
    global.nvda_init = external_define(""nvda_gm.dll"", ""gmnvda_init"", 0, 0, 1, 1);
    global.nvda_speak = external_define(""nvda_gm.dll"", ""gmnvda_speak"", 0, 0, 1, 1);
    external_call(global.nvda_init, """");
    global.nvda_ready = 1;
}";

var importGroup = new UndertaleModLib.Compiler.CodeImportGroup(Data);

// Create: init the bridge, speak line 0, remember it as already-spoken.
importGroup.QueueAppend(Data.Code.ByName("gml_Object_obj_base_writer_Create_0"),
    bridgeInit + "\nnvda_lastspoke = stringno;\n" + SpeakCore("mystring[0]"));

// Step (every frame): if the line index changed, speak the new line.
importGroup.QueueAppend(Data.Code.ByName("gml_Object_obj_base_writer_Step_0"),
    "if (variable_instance_exists(id, \"nvda_lastspoke\") && stringno != nvda_lastspoke)\n{\n    nvda_lastspoke = stringno;\n"
    + SpeakCore("mystring[stringno]") + "\n}");

// OBJ_NOMSCWRITER (the enemy-turn speech-bubble writer) defines its OWN Create_0
// that does NOT call event_inherited(), so the base-writer hook above never runs
// for it -> creatures' turn comments went unspoken.  Hook its Create directly with
// the same logic.  Its Step_0 is inherited from obj_base_writer, so the line-change
// watcher above already covers advancing lines once nvda_lastspoke is set here.
importGroup.QueueAppend(Data.Code.ByName("gml_Object_OBJ_NOMSCWRITER_Create_0"),
    bridgeInit + "\nnvda_lastspoke = stringno;\n" + SpeakCore("mystring[0]"));

importGroup.Import();
Console.WriteLine("Injected NVDA: base_writer Create_0 + Step_0 watcher + NOMSCWRITER Create_0");

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
                _say = ""Load menu. Left or right for Continue or Reset, down for Settings. On "" + _o3;
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
            }
            else
            {
                if (selected3 == 0) _say = ""Begin Game"";
                else if (selected3 == 1) _say = ""Settings"";
            }
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
                  "ds_list_replace","ds_list_destroy","string_copy","string_length","string_replace_all" };
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
    with (obj_doorparent) ds_list_add(_list, id);
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
{" + bridge + @"
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
            external_call(global.nvda_speak, string(global.nvda_idx + 1) + "" of "" + string(global.nvda_listn) + "". "" + _out);
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
        if (nvda_startdelay > 0)
        {
            nvda_startdelay -= 1;
        }
        else if (variable_global_exists(""nvda_sel"") && global.nvda_sel != noone && instance_exists(global.nvda_sel))
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
        if (_tried && x == xprevious && y == yprevious)
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
                  "string","string_pos","string_copy","string_length","string_char_at","round" };
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
    if (!variable_instance_exists(id, ""nvda_bset""))
    {
        nvda_bset = 1; nvda_pmenu = -99; nvda_pc0 = -99; nvda_pc1 = -99;
        nvda_pc2 = -99; nvda_pc3 = -99; nvda_pc4 = -99; nvda_pinmenu = 0; nvda_prefix = """";
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

    // Target select (FIGHT / ACT): monster name + HP.
    if (global.bmenuno == 1 || global.bmenuno == 2 || global.bmenuno == 11)
    {
        if (global.bmenucoord[1] != nvda_pc1)
        {
            nvda_pc1 = global.bmenucoord[1];
            var _nm = global.monstername[nvda_pc1];
            var _hp = """";
            if (global.monstermaxhp[nvda_pc1] > 0) _hp = "", H P "" + string(global.monsterhp[nvda_pc1]) + "" of "" + string(global.monstermaxhp[nvda_pc1]);
            external_call(global.nvda_speak, nvda_prefix + _nm + _hp);
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

    // MERCY: Spare / Flee.
    if (global.bmenuno == 4)
    {
        if (global.bmenucoord[4] != nvda_pc4)
        {
            nvda_pc4 = global.bmenucoord[4];
            var _m = ""Spare"";
            if (nvda_pc4 == 1) _m = ""Flee"";
            external_call(global.nvda_speak, nvda_prefix + _m);
            nvda_prefix = """";
        }
    }
}";

var g = new UndertaleModLib.Compiler.CodeImportGroup(Data);
g.QueueAppend(Data.Code.ByName("gml_Object_obj_battlecontroller_Draw_0"), gml);
g.Import();
Console.WriteLine("Injected battle menu announcer (buttons/target/act/item/mercy + HP)");

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
{" + bridge + @"
    hspeed *= 0.5;
    nvda_beeptimer = 0;
    nvda_announced = 0;
    external_call(global.nvda_speak, ""Attack. Press Z at the highest beep."");
}";

string stepGml = @"
{
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
    "obj_croissant", "obj_vertcroissant", "obj_spiderbullet"
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
            global.nvda_dlid[global.nvda_doorn] = id;
            global.nvda_dld[global.nvda_doorn] = point_distance(x, y, other.x, other.y);
            global.nvda_doorn += 1;
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
{" + bridge + @"
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
