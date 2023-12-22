using System;
using System.Threading.Tasks;
using SQLite;

namespace PoliceUtils.Utils
{
    public class Config
    {
        [PrimaryKey]
        public string confKey { get; set; }
        
        public string confValue { get; set; }


        public async static Task<bool> InitDefaultValues()
        {
            SQLiteAsyncConnection db = PoliceSQLUtil.db;
            
            // Add item "menu.key" if not exists
            db.ExecuteAsync("INSERT OR IGNORE INTO Config (confKey, confValue) VALUES ('menu.key', 'P')");

            return true;
        }
        
        public async static Task<string> Get(string key)
        {
            SQLiteAsyncConnection db = PoliceSQLUtil.db;
            
            Config config = await db.Table<Config>().Where((Config c) => c.confKey == key).FirstOrDefaultAsync();
            if (config == null)
            {
                return null;
            }

            return config.confValue;
        }
    }
}