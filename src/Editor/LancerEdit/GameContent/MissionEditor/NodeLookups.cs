using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Schema.Missions;

namespace LancerEdit.GameContent.MissionEditor;

public ref struct NodeLookups
{
    public MissionScriptDocument MissionIni;

    private string[] _ships;

    public string[] Ships
    {
        get
        {
            if (_ships == null)
            {
                _ships = MissionIni.Ships.Keys.ToArray();
            }
            return _ships;
        }
    }

    private string[] _solars;

    public string[] Solars
    {
        get
        {
            if (_solars == null)
            {
                _solars = MissionIni.Solars.Keys.ToArray();
            }
            return _solars;
        }
    }


    private string[] _shipsAndSolars;

    public string[] ShipsAndSolars
    {
        get
        {
            if (_shipsAndSolars == null)
            {
                _shipsAndSolars = MissionIni.Ships.Keys
                    .Concat(MissionIni.Solars.Keys).Order().ToArray();
            }
            return _shipsAndSolars;
        }
    }

    private string[] _labels;

    public string[] Labels
    {
        get
        {
            if (_labels == null)
            {
                _labels = MissionIni.Ships.Values.SelectMany(x => x.Labels).Distinct().ToArray();
            }
            return _labels;
        }
    }

    private string[] _shipsAndLabels;

    public string[] ShipsAndLabels
    {
        get
        {
            if (_shipsAndLabels == null)
            {
                _shipsAndLabels = Ships.Concat(Labels).ToArray();
            }
            return _shipsAndLabels;
        }
    }
    public string[] ShipsSolarsAndLabels
    {
        get
        {
            return ShipsAndLabels.Concat(ShipsAndSolars).Distinct().ToArray();
        }
    }
    private string[] _objectives;

    public string[] Objectives
    {
        get
        {
            if (_objectives == null)
            {
                _objectives = MissionIni.Objectives.Keys.ToArray();
            }
            return _objectives;
        }
    }

    private string[] _formations;

    public string[] Formations
    {
        get
        {
            if (_formations == null)
            {
                _formations = MissionIni.Formations.Keys.ToArray();
            }
            return _formations;
        }
    }

    private string[] _loots;

    public string[] Loots
    {
        get
        {
            if (_loots == null)
            {
                _loots = MissionIni.Loots.Keys.ToArray();
            }

            return _loots;
        }
    }

    private string[] _objLists;

    public string[] ObjLists
    {
        get
        {
            if (_objLists == null)
            {
                _objLists = MissionIni.ObjLists.Keys.ToArray();
            }
            return _objLists;
        }
    }

    private string[] _dialogs;

    public string[] Dialogs
    {
        get
        {
            if (_dialogs == null)
            {
                _dialogs = MissionIni.Dialogs.Keys.ToArray();
            }
            return _dialogs;
        }
    }
}
