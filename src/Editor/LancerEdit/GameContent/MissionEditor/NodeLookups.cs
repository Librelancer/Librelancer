using System.Linq;
using LibreLancer.Data.Schema.Missions;

namespace LancerEdit.GameContent.MissionEditor;

public ref struct NodeLookups
{
    public MissionIni MissionIni;

    private string[] _ships;

    public string[] Ships
    {
        get
        {
            if (_ships == null)
            {
                _ships = MissionIni.Ships.Select(x => x.Nickname).Order().ToArray();
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
                _solars = MissionIni.Solars.Select(x => x.Nickname).Order().ToArray();
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
                _shipsAndSolars = MissionIni.Ships.Select(x => x.Nickname)
                    .Concat(MissionIni.Solars.Select(x => x.Nickname)).Order().ToArray();
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
                _labels = MissionIni.Ships.SelectMany(x => x.Labels).ToArray();
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

    private string[] _objectives;

    public string[] Objectives
    {
        get
        {
            if (_objectives == null)
            {
                _objectives = MissionIni.Objectives.Select(x => x.Nickname).Order().ToArray();
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
                _formations = MissionIni.Formations.Select(x => x.Nickname).Order().ToArray();
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
                _loots = MissionIni.Loots.Select(x => x.Nickname).Order().ToArray();
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
                _objLists = MissionIni.ObjLists.Select(x => x.Nickname).Order().ToArray();
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
                _dialogs = MissionIni.Dialogs.Select(x => x.Nickname).Order().ToArray();
            }
            return _dialogs;
        }
    }
}
