using SQLite;

namespace PoliceUtils.Utils
{
    class Wanted
    {
        [AutoIncrement]
        [PrimaryKey]
        public int id
        {
            get;
            set;
        }

        public string name
        {
            get;
            set;
        }

        public string reason
        {
            get;
            set;
        }
    }
}
