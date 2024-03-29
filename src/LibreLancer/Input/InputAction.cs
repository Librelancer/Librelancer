﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Input
{
    //All vanilla + JFLP actions here
    public enum InputAction
    {
        USER_AFTERBURN,
        USER_AFTERBURN_OFF,
        USER_AFTERBURN_ON,
        USER_ASSIGN_WEAPON_GROUP1,
        USER_ASSIGN_WEAPON_GROUP2,
        USER_ASSIGN_WEAPON_GROUP3,
        USER_ASSIGN_WEAPON_GROUP4,
        USER_ASSIGN_WEAPON_GROUP5,
        USER_ASSIGN_WEAPON_GROUP6,
        USER_AUTO_TURRET,
        USER_CANCEL,
        USER_CHASE_CAMERA_MODE,
        USER_CHAT,
        USER_CHAT_WINDOW,
        USER_CLEAR_TARGET,
        USER_CLOSEST_ENEMY,
        USER_COCKPIT_CAMERA_MODE,
        USER_COLLECT_LOOT,
        USER_CONTACT_LIST,
        USER_CRUISE,
        USER_DEC_THROTTLE,
        USER_DISPLAY_LAST_OBJECTIVE,
        USER_ENTER,
        USER_FIRE_FORWARD,
        USER_FIRE_WEAPON1,
        USER_FIRE_WEAPON2,
        USER_FIRE_WEAPON3,
        USER_FIRE_WEAPON4,
        USER_FIRE_WEAPON5,
        USER_FIRE_WEAPON6,
        USER_FIRE_WEAPON7,
        USER_FIRE_WEAPON8,
        USER_FIRE_WEAPON9,
        USER_FIRE_WEAPON10,
        USER_FIRE_WEAPON_GROUP1,
        USER_FIRE_WEAPON_GROUP2,
        USER_FIRE_WEAPON_GROUP3,
        USER_FIRE_WEAPON_GROUP4,
        USER_FIRE_WEAPON_GROUP5,
        USER_FIRE_WEAPON_GROUP6,
        USER_FIRE_WEAPONS,
        USER_FORMATION_LIST,
        USER_FULLSCREEN,
        USER_GROUP_INVITE,
        USER_GROUP_WINDOW,
        USER_HELP,
        USER_INC_THROTTLE,
        USER_INVENTORY,
        USER_LAUNCH_COUNTERMEASURES,
        USER_LAUNCH_CRUISE_DISRUPTORS,
        USER_LAUNCH_MINES,
        USER_LAUNCH_MISSILES,
        USER_LAUNCH_TORPEDOS,
        USER_LOOK_ROTATE_CAMERA_DOWN,
        USER_LOOK_ROTATE_CAMERA_LEFT,
        USER_LOOK_ROTATE_CAMERA_RESET,
        USER_LOOK_ROTATE_CAMERA_RIGHT,
        USER_LOOK_ROTATE_CAMERA_UP,
        USER_MANEUVER_BRAKE_REVERSE,
        USER_MANEUVER_DOCK,
        USER_MANEUVER_ENGINEKILL,
        USER_MANEUVER_FORMATION,
        USER_MANEUVER_FREE_FLIGHT,
        USER_MANEUVER_GOTO,
        USER_MANEUVER_SLIDE_EVADE_DOWN,
        USER_MANEUVER_SLIDE_EVADE_LEFT,
        USER_MANEUVER_SLIDE_EVADE_RIGHT,
        USER_MANEUVER_SLIDE_EVADE_UP,
        USER_MINIMIZE_HUD,
        USER_NAV_MAP,
        USER_NEXT_ENEMY,
        USER_NEXT_ITEM,
        USER_NEXT_OBJECT,
        USER_NEXT_SUBTARGET,
        USER_NEXT_TARGET,
        USER_NN,
        USER_NO,
        USER_PAUSE,
        USER_PLAYER_STATS,
        USER_PREV_ENEMY,
        USER_PREV_ITEM,
        USER_PREV_OBJECT,
        USER_PREV_SUBTARGET,
        USER_PREV_TARGET,
        USER_RADIO,
        USER_REARVIEW_CAMERA_MODE,
        USER_REMAPPABLE_DOWN,
        USER_REMAPPABLE_LEFT,
        USER_REMAPPABLE_RIGHT,
        USER_REMAPPABLE_UP,
        USER_REPAIR_HEALTH,
        USER_REPAIR_SHIELD,
        USER_SAVE_GAME,
        USER_SCAN_CARGO,
        USER_SCREEN_SHOT,
        USER_STATUS,
        USER_STOP,
        USER_STORY_STAR,
        USER_SWITCH_TO_TARGET,
        USER_TARGET,
        USER_THRUST,
        USER_TOGGLE_AUTO_LEVEL,
        USER_TOGGLE_LEVEL_CAMERA,
        USER_TOGGLE_WEAPON1,
        USER_TOGGLE_WEAPON2,
        USER_TOGGLE_WEAPON3,
        USER_TOGGLE_WEAPON4,
        USER_TOGGLE_WEAPON5,
        USER_TOGGLE_WEAPON6,
        USER_TOGGLE_WEAPON7,
        USER_TOGGLE_WEAPON8,
        USER_TOGGLE_WEAPON9,
        USER_TOGGLE_WEAPON10,
        USER_TRACTOR_BEAM,
        USER_TRADE_REQUEST,
        USER_TURN_SHIP,
        USER_VIEW_RESET,
        USER_WARP,
        USER_WEAPON_GROUP1,
        USER_WEAPON_GROUP2,
        USER_WEAPON_GROUP3,
        USER_WEAPON_GROUP4,
        USER_WEAPON_GROUP5,
        USER_WEAPON_GROUP6,
        USER_X_ROTATE,
        USER_X_UNROTATE,
        USER_YES,
        USER_Y_ROTATE,
        USER_Y_UNROTATE,
        USER_ZOOM_IN,
        USER_ZOOM_OUT,
        USER_Z_ROTATE,
        USER_Z_UNROTATE,
        //FL debug (unused)
        DBG_ALTER_CAMERA_FORWARD,
        DBG_ALTER_CAMERA_BACK,
        DBG_ALTER_CAMERA_UP,
        DBG_ALTER_CAMERA_DOWN,
        DBG_INFO,
        DBG_INFO_2,
        DBG_INTERFACE_VIEW,
        DBG_BLINK_WARP,
        DBG_TEST_EFFECT,
        DBG_DESTROY_TARGET,
        DBG_AI_TOOL,
        DBG_CONSOLE,
        DBG_OPTIONS,
        DBG_TEXTURES,
        DBG_FPS,
        DBG_FPS_GRAPH,
        DBG_NAVMAP_TELEPORT,
        DBG_NAVMAP_DRAW_ZONES,
        DBG_NAVMAP_DRAW_SHIPS,
        DBG_NAVMAP_SHOW_ALL,
        DBG_SHOW_MISSION,
        DBG_FIRE_FORWARD,
        //Count of elements
        COUNT
    }

    public struct InputBinding
    {
        public UserInput Primary;
        public UserInput Secondary;

        public override string ToString()
        {
            if (Secondary.NonEmpty) {
                return $"{Primary} ({Secondary})";
            }
            return Primary.ToString();
        }
    }
}