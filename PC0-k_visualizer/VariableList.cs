namespace PC0
{
    internal class VariableList<T> : List<T>
    {
        int hash;
        public int ID { get; private set;  }
        public string identifier { get; private set; }

        public VariableList() : base() 
        {
            ID = -1;
            identifier = "this wasn't defined :(";
        }

        public VariableList(VariableList<T> list, int ID, string identifier = "") : base(list) 
        {
            SetHashCode();
            this.ID = ID;
            if (identifier == "")
                this.identifier = ID.ToString();
            else
                this.identifier = identifier;
        }

        public void SetHashCode()
        {
            hash = string.Join(string.Empty, this).GetHashCode();
        }

        public override int GetHashCode()
        {
            return hash;
        }

        public override bool Equals(object? obj)
        {
            return 
                obj is VariableList<T> other &&
                other.hash == hash;
        }

        public override string ToString()
        {
            return string.Join(string.Empty, this);
        }
    }
}
