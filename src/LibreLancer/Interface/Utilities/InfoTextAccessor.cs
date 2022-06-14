// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Interface
{
    //Helper class for using Strids with fallback
    class InfoTextAccessor
    {
        private int _strid;
        private int _infoid;
        public string Text { get; set; }

        public int Strid
        {
            get { return _strid; }
            set
            {
                _strid = value;
                _idsTried = false;
                _idsText = null;
            }
        }

        public int InfoId
        {
            get { return _infoid; }
            set
            {
                _infoid = value;
                _idsTried = false;
                _idsText = null;
            }
        }

        private bool _allCaps = false;
        public bool AllCaps
        {
            get => _allCaps;
            set
            {
                _allCaps = value;
                _idsTried = false;
                _idsText = null;
            }
        }
        
        private string _idsText = null;
        private bool _idsTried = false;
        public string GetText(UiContext context)
        {
            if (Strid == 0 && InfoId == 0) return Text;
            if (context.Data.Infocards == null) return Text;
            if (_idsText != null) return _idsText;
            if (_idsTried) return Text;
            if (Strid != 0) {
                _idsTried = true;
                _idsText = context.Data.Infocards.GetStringResource(Strid);
                if (!string.IsNullOrEmpty(_idsText))
                {
                    if (_allCaps) _idsText = _idsText.ToUpper();
                    return _idsText;
                }
            }
            if (InfoId != 0) {
                _idsTried = true;
                var xml = context.Data.Infocards.GetXmlResource(InfoId);
                if (xml == null) return Text;
                var icard = Infocards.RDLParse.Parse(xml, context.Data.Fonts);
                _idsText = icard.ExtractText().TrimEnd();
                _idsText = _idsText.ToUpper();
                return _idsText;
            }
            return Text;
        }
    }
}