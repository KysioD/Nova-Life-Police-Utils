using SQLite;
namespace PoliceUtils.Utils
{
    class Armury
    {
        [AutoIncrement]
        [PrimaryKey]
        public int id
        {
            get;
            set;
        }

        public int gun
        {
            get;
            set;
        }

        public int ammo
        {
            get;
            set;
        }

        public int tazer
        {
            get;
            set;
        }

        public int characterid
        {
            get;
            set;
        }

        public string gunDatas
        {
            get;
            set;
        }
    }
}
