// IDs referenced by UI code
STRID_BUY = 3016
STRID_SELL = 3017
STRID_CREDITS = 1142

STRID_SHIP_PRICE = 969
STRID_SHIP_NEEDMONEY = 1584

STRID_NEG_CREDITS = 1174
STRID_ADD_CREDITS = 1175
STRID_CREDIT_SIGN = 8512

STRID_INSUFFICIENT_SPACE = 1150
STRID_INSUFFICIENT_CREDITS = 1151
STRID_NO_SHIP = 1152

STRID_NEW_CHARACTER = 1874
STRID_CRUISE_CHARGING = 1413

STRID_MISSION = 1350


STRID_RETURN_TO_GAME = 1419

STRID_GAME_OVER = 1826
STRID_YOU_ARE_DEAD = 1827

// Mp Related ids

STRID_DISCONNECT = 1847
STRID_NAME_TAKEN = 1848
STRID_BANNED = 1858
STRID_PASSWORD_PROMPT = 1872
STRID_INCORRECT_PASSWORD = 1849
STRID_ALREADY_LOGGED_IN = 1875


// Ship Classes

STRID_CLS_LF = 923
STRID_CLS_HF = 924
STRID_CLS_FR = 925
STRID_CLS_VHF = 926

// Initialiser with default values included here for compatibility with the adoxa shipclass plugin

ShipClassNames = {
    "Light Fighter",
    "Heavy Fighter",
    "Freighter",
    "Very Heavy Fighter",
    "Gunboat",
    "Cruiser",
    "Destroyer",
    "Battleship",
    "Capital",
    "Transport",
    "Large Transport",
    "Train",
    "Large Train",
    "",
    "",
    "",
    "",
    "",
    "",
    ""
}

function LoadShipClassNames()
{
	ShipClassNames[1] = StringFromID(STRID_CLS_LF)
	ShipClassNames[2] = StringFromID(STRID_CLS_HF)
	ShipClassNames[3] = StringFromID(STRID_CLS_FR)
	ShipClassNames[4] = StringFromID(STRID_CLS_VHF)
}







