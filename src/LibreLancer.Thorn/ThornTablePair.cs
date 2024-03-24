namespace LibreLancer.Thorn
{
    public struct ThornTablePair
    {
        private object key, value;
        public object Key
        {
            get { return key; }
            private set { Key = key; }
        }

        public object Value
        {
            get { return value; }
            set { if (key != null) Value = value; }
        }

        public ThornTablePair(object key, object val)
        {
            this.key = key;
            this.value = val;
        }
    }
}
