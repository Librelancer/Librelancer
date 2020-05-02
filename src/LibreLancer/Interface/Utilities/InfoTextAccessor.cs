// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Interface
{
    //Helper class for using Strids with fallback
    class InfoTextAccessor
    {
        public string Text { get; set; }
        public int Strid { get; set; }
        public int InfoId { get; set; }
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
                if (!string.IsNullOrEmpty(_idsText)) return _idsText;
            }
            if (InfoId != 0) {
                _idsTried = true;
                var xml = context.Data.Infocards.GetXmlResource(InfoId);
                if (xml == null) return Text;
                var icard = Infocards.RDLParse.Parse(xml, context.Data.Fonts);
                _idsText = icard.ExtractText().TrimEnd();
                return _idsText;
            }
            return Text;
        }
    }
}