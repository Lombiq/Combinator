namespace Piedone.Combinator.SpriteGenerator.Utility
{
    //Bit struct. In reference, DFS-order is stored in a 0-1 sequence, 
    //this struct has been created to follow terminology.
    internal struct Bit
    {
        private bool _bit;

        private Bit(int n)
        {
            _bit = (n == 1) ? true : false;
        }

        public static implicit operator Bit(int n)
        {
            return new Bit(n);
        }

        public override bool Equals(object obj)
        {
            if (obj is Bit)
                return this == ((Bit)obj);
            else if (obj is int)
                return this == (int)obj;

            return false;
        }

        public override int GetHashCode()
        {
            return _bit.GetHashCode();
        }

        public static bool operator ==(Bit b, int n)
        {
            if ((b._bit == true && n == 1) || (b._bit == false && n == 0))
                return true;
            else return false;
        }

        public static bool operator !=(Bit b, int n)
        {
            if (!((b._bit == true && n == 1) || (b._bit == false && n == 0)))
                return true;
            else return false;
        }

        public static bool operator ==(Bit b1, Bit b2)
        {
            if ((b1._bit == true && b2._bit == true) || (b1._bit == false && b2._bit == false))
                return true;
            else return false;
        }

        public static bool operator !=(Bit b1, Bit b2)
        {
            if (!((b1._bit == true && b2._bit == true) || (b1._bit == false && b2._bit == false)))
                return true;
            else return false;
        }
    }
}
