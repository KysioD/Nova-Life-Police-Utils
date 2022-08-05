using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using UnityEngine;

namespace PoliceUtils.Utils
{
    class PoliceSQLUtil
    {
        public static SQLiteAsyncConnection db;

        public async static Task<bool> Init(string pluginPath)
        {
            PoliceSQLUtil.db = new SQLiteAsyncConnection(PolicePlugin.policeDatabasePath);
            await PoliceSQLUtil.db.CreateTableAsync<Armury>(CreateFlags.None);
            Debug.Log("[PolicePlugin] Database init");
            return true;
        }

        public async static Task<bool> registerArmury(bool gun, bool tazer, int ammo, string gunDatas, int characterid)
        {
            Armury armury = new Armury()
            {
                gun = gun ? 1 : 0,
                tazer = tazer ? 1 : 0,
                ammo = ammo,
                characterid = characterid,
                gunDatas = gunDatas
            };

            await PoliceSQLUtil.db.InsertAsync(armury);
            return true;
        }

        public async static Task<Armury> getArmury(int characterid)
        {
            AsyncTableQuery<Armury> asyncTableQuery = PoliceSQLUtil.db.Table<Armury>();
            Armury armury = await asyncTableQuery.Where((Armury a) => a.characterid == characterid).FirstOrDefaultAsync();

            return armury;
        }

        public async static Task<bool> removeArmury(int characterid)
        {
            AsyncTableQuery<Armury> asyncTableQuery = PoliceSQLUtil.db.Table<Armury>();
            await asyncTableQuery.DeleteAsync((Armury a) => a.characterid == characterid);

            return true;
        }

        public async static Task<Wanted> getWanted(string name)
        {
            AsyncTableQuery<Wanted> asyncTableQuery = PoliceSQLUtil.db.Table<Wanted>();
            Wanted wanted = await asyncTableQuery.Where((Wanted w) => w.name == name).FirstOrDefaultAsync();

            return wanted;
        }

        public async static Task<List<Wanted>> getAllWanted()
        {
            AsyncTableQuery<Wanted> asyncTableQuery = PoliceSQLUtil.db.Table<Wanted>();
            List<Wanted> wanted = await asyncTableQuery.ToListAsync();

            return wanted;
        }

        public async static Task<bool> addWanted(string name, string reason)
        {
            Wanted wanted = new Wanted()
            {
                name = name,
                reason = reason
            };

            await PoliceSQLUtil.db.InsertAsync(wanted);

            return true;
        } 

        public async static Task<bool> removeWanted(string name)
        {
            AsyncTableQuery<Wanted> asyncTableQuery = PoliceSQLUtil.db.Table<Wanted>();
            await asyncTableQuery.DeleteAsync((Wanted w) => w.name == name);

            return true;
        }
    }
}
