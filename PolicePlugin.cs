using Life;
using Life.BizSystem;
using Life.CheckpointSystem;
using Life.DB;
using Life.Network;
using Life.UI;
using Life.VehicleSystem;
using Mirror;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PoliceUtils.Utils;
using System.Collections;
using Life.InventorySystem;
using static Life.Network.LifeServer;

namespace PoliceUtils
{
    class PolicePlugin : Plugin
    {
        public static string policeDirectoryPath;
        public static string policeDatabasePath;
        private LifeServer server;


        public PolicePlugin(IGameAPI api) : base(api)
        { 
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            policeDirectoryPath = $"{pluginsPath}/PolicePlugin";
            policeDatabasePath = policeDirectoryPath + "/police.db";

            if (!Directory.Exists(policeDirectoryPath))
            {
                Directory.CreateDirectory(policeDirectoryPath);
            }

            if (!File.Exists(policeDatabasePath))
            {
                Debug.LogError("[PolicePlugin] Any database found for " + policeDirectoryPath + " please add one !");
                return;
            }

            initDatabase();

            server = Nova.server;
        }

        public override void OnPlayerSpawnCharacter(Player player, NetworkConnection conn, Characters character)
        {
            base.OnPlayerSpawnCharacter(player, conn, character);

            RegisterCheckpoints(player);
        }

        public override void OnPlayerInput(Player player, KeyCode keycode, bool onUI)
        {
            base.OnPlayerInput(player, keycode, onUI);
            if (keycode == KeyCode.P && !onUI)
            {
                UIPanel platePanel = new UIPanel("Rechercher une plaque", UIPanel.PanelType.Input)
                    .AddButton("Fermer", (ui) =>
                    {
                        player.ClosePanel(ui);
                    })
                    .AddButton("Rechercher", (ui) =>
                    {
                        string plate = ui.inputText;

                        LifeVehicle vehicle = Nova.v.GetVehicle(plate);

                        if (vehicle != null)
                        {
                            Player target = server.GetPlayer(vehicle.permissions.owner.characterId);
                            if (target != null)
                            {
                                string name = target.GetFullName(); ;

                                player.SendText("<color=#4dfaf8>Le propriétaire de ce véhicule est : " + name + " !</color>");

                                ui.SelectTab();
                            }
                            else
                            {
                                player.SendText("<color=#fb4039>Impossible de trouvre le propriétaire de ce véhicule !</color>");
                            }
                        }
                        else
                        {
                            player.SendText("<color=#fb4039>Aucun véhicule avec cette plaque d'immatriculation n'a été trouvé</color>");
                        }
                        player.ClosePanel(ui);
                    });

                UIPanel wantedPanel = new UIPanel("Avis de recherche", UIPanel.PanelType.Input)
                    .AddButton("Fermer", (ui) =>
                    {
                        player.ClosePanel(ui);
                    })
                    .AddButton("Lancer une recherche", (ui) =>
                    {
                        string name = ui.inputText;

                        UIPanel reasonPanel = new UIPanel("Raison de l'avis de recherche", UIPanel.PanelType.Input)
                        .AddButton("Fermer", (ui2) =>
                        {
                            player.ClosePanel(ui2);
                        })
                        .AddButton("Valider", (ui2) =>
                        {
                            string reason = ui2.inputText;

                            foreach (Player p in Nova.server.Players)
                            {
                                if(p.HasBiz() && isActivity(p, Activity.Type.LawEnforcement)){ 
                                    p.SendText("<color=#00bfbf>" + name + " est désormais recherché pour " + reason + "</color>");
                                }
                            }
                            player.ClosePanel(ui2);
                            PoliceSQLUtil.addWanted(name.ToUpper(), reason);
                        });

                        player.ClosePanel(ui);
                        player.ShowPanelUI(reasonPanel);
                    })
                    .AddButton("Chercher", (ui) =>
                    {
                        verifyWanted(ui.inputText, player);
                        player.ClosePanel(ui);
                    })
                    .AddButton("Retirer", (ui) =>
                    {
                        string remove = ui.inputText;
                        removeWanted(remove.ToUpper(), player);
                        player.ClosePanel(ui);
                    })
                    .AddButton("Liste", (ui) => {
                        UIPanel listWasted = new UIPanel("List of Wasted people", UIPanel.PanelType.Tab)
                            .AddButton("Fermer", (ui2) =>
                            {
                                player.ClosePanel(ui2);
                            })
                            .AddButton("Sélectionner", (ui2) =>
                            {
                                ui2.SelectTab();
                                player.ClosePanel(ui2);
                            });

                        listAllWanted(player, listWasted);
                        player.ClosePanel(ui);
                    });


                UIPanel policePanel = new UIPanel("Police", UIPanel.PanelType.Tab)
                    .AddButton("Fermer", (ui) =>
                    {
                        player.ClosePanel(ui);
                    })
                    .AddButton("Sélectionner", (ui) =>
                    {
                        ui.SelectTab();
                    })
                    .AddTabLine("Controler la plaque", (ui) =>
                    {
                        player.ClosePanel(ui);
                        player.ShowPanelUI(platePanel);
                    })
                    .AddTabLine("Dépistage de stupéfiant", (ui) =>
                    { 
                        player.ClosePanel(ui);

                        Player target = player.GetClosestPlayer();
                        if (target == null)
                        {
                            player.SendText("<color=#fb4039>Il n'y a personne à dépister autour de vous.</color>");
                            return;
                        }

                        server.SendLocalText("<color=#43fdfa>L'agent de police " + player.GetFullName() + " fait un dépistage de stupéfiant à " + target.GetFullName()+"</color>", 2f, player.setup.transform.position);

                        Nova.server.lifeManager.StartCoroutine(testDrug(player, target));

                    })
                    .AddTabLine("Avis de recherche", (ui) =>
                    {
                        player.ClosePanel(ui);
                        player.ShowPanelUI(wantedPanel);
                    })
                    .AddTabLine("Contrôle bancaire", (ui) =>
                    {
                        Player target = player.GetClosestPlayer();
                        if(target != null)
                        {
                            player.SendText("<color=#3efeff>" + target.GetFullName() + " a " + target.character.Bank + "€ dans son compte en banque !</color>");
                        }
                        else
                        {
                            player.SendText("<color=#fb4039>Il n'y a personne à proximité !</color>");
                        }
                    })
                    /*.AddTabLine("Verbaliser un véhicule", (ui) =>
                    {
                        Vehicle vehicle = player.GetClosestVehicle();
                        if(vehicle != null)
                        {
                        }
                        else
                        {
                            player.SendText("<color=#FF0000>Il n'y a aucun véhicule à proximité !</color>");
                        }
                    })*/
                    .AddTabLine("Contrôle d'argent liquide", (ui) =>
                    {
                        Player target = player.GetClosestPlayer();
                        if (target != null)
                        {
                            player.SendText("<color=#3efeff>" + target.GetFullName() + " a " + target.character.Money + "€ sur lui !</color>");
                        }
                        else
                        {
                            player.SendText("<color=#fb4039>Il n'y a personne à proximité !</color>");
                        }
                    });
                

                if (player.HasBiz() && isActivity(player, Activity.Type.LawEnforcement))
                {
                    player.ShowPanelUI(policePanel);
                }

            }
        }

        public IEnumerator testDrug(Player player, Player target)
        {
            yield return new WaitForSeconds(2f);
            Nova.server.SendLocalText("<color=#fc403f>" + target.GetFullName() + " " + (target.setup.isDruged ? "est positif" : "est négatif") + " au dépistage de stupéfiant</color>", 2f, player.setup.transform.position);
        }

        public async void listAllWanted(Player player, UIPanel panel)
        {
            List<Wanted> wanted = await PoliceSQLUtil.getAllWanted();

            foreach (Wanted w in wanted)
            {
                panel.AddTabLine(w.name, (ui) =>
                {
                    player.SendText("<color=#00bfbf>" + w.name + " est recherché pour " + w.reason + "</color>");
                    //player.SendText(w.name + "<color=#fb4039> est recherché pour </color>" + w.reason);
                });
            }
            player.ShowPanelUI(panel);
        }
        public async void removeWanted(string name, Player player)
        {
            Wanted wanted = await PoliceSQLUtil.getWanted(name.ToUpper());
            if(wanted != null)
            {
                await PoliceSQLUtil.removeWanted(name.ToUpper());
                foreach(Player p in Nova.server.Players)
                {
                    if (p.biz.IsActivity(Activity.Type.LawEnforcement))
                    {
                        p.SendText("<color=#44ff3a>" + name + " n'est plus recherché par la police !");
                    }
                }
            }
            else
            {
                player.SendText("<color=#fc403f>" + name + " n'est pas recherché par la police !");
            }
        }

        public async void verifyWanted(string name, Player player)
        {
            Wanted wanted = await PoliceSQLUtil.getWanted(name.ToUpper());
            if(wanted == null)
            {
                player.SendText("<color=#fc403f>" + name + " n'est pas recherché par la police !");
            }
            else
            {
                player.SendText("<color=#fc403f>" + name + " est recherché par la police pour "+wanted.reason);
            }
        }

        private bool isActivity(Player player, Activity.Type type)
        {
            if (!player.HasBiz()) return false;

            foreach (Activity.Type activity in Nova.biz.GetBizActivities(player.biz.Id))
            {
                if (activity == type)
                {
                    return true;
                }
            }

            return false;
        }

        private void RegisterCheckpoints(Player player)
        {
            NCheckpoint policeArmuryCheckpoint = new NCheckpoint(player.netId, new Vector3(362f, 50.03f, 633.8f), (checkpoint) =>
            {
                UIPanel armuryPanel = new UIPanel("Armury", UIPanel.PanelType.Text)
                .SetText("Récupérez ou rendez votre équipement. Attention, tout équipement perdu ne sera redonné.")
                .AddButton("Rendre l'équipement", (ui) =>
                {


                    bool gun = (player.setup.inventory.GetItemSlotById(6) >= 0);
                    string gunDatas = (gun ? player.setup.inventory.items[player.setup.inventory.GetItemSlotById(6)].data : "");
                    bool tazer = (player.setup.inventory.GetItemSlotById(36) >= 0);
                    int ammoSlot = player.setup.inventory.GetItemSlotById(7);
                    int ammo = 0;
                    if(ammoSlot >= 0) { 
                        ammo = player.setup.inventory.items[ammoSlot].number;
                    }
                    registerArmury(gun, tazer, ammo, gunDatas, player);

                    player.setup.inventory.RemoveItem(6, Int32.MaxValue, true); //Arme
                    player.setup.inventory.RemoveItem(7, Int32.MaxValue, true); //Munitions
                    player.setup.inventory.RemoveItem(36, Int32.MaxValue, true); //Tazer
                    player.ClosePanel(ui);
                    /**int gunSlot = player.setup.inventory.LookForItem(6);
                    if (gunSlot > -1)
                    {
                        player.setup.inventory.RemoveItemBySlotId(gunSlot, 1, true);
                    }

                    int ammoSlot = player.setup.inventory.LookForItem(7);
                    int count = player.setup.inventory.items[ammoSlot].number;
                    if(ammoSlot > -1)
                    {
                        player.setup.inventory.RemoveItem()
                    }**/
                })
                .AddButton("Prendre l'équipement", (ui) =>
                {
                    getArmury(player, ui);
                })
                .AddButton("Fermer", (ui) =>
                {
                    player.ClosePanel(ui);
                });

                player.ShowPanelUI(armuryPanel);
            });

            player.CreateCheckpoint(policeArmuryCheckpoint);
        }

        public async void getArmury(Player player, UIPanel ui)
        {
            int characterId = player.character.Id;
            Armury armury = await PoliceSQLUtil.getArmury(characterId);
            bool gun = false;
            bool tazer = false;
            bool ammo = false;
            int ammo_amount = armury.ammo;
            string gunDatas = armury.gunDatas;

            if (armury.gun == 1) gun = !player.setup.inventory.AddItem(6, 1, gunDatas);
            if (armury.tazer == 1) tazer = !player.setup.inventory.AddItem(36, 1, "");
            ammo = !player.setup.inventory.AddItem(7, ammo_amount, "");

            await PoliceSQLUtil.removeArmury(player.character.Id);

            if (gun || tazer || ammo)
            {
                if (!ammo) ammo_amount = 0;
                registerArmury(gun, tazer, ammo_amount, gunDatas, player);
            }

            player.ClosePanel(ui);

        }
        public async void registerArmury(bool gun, bool tazer, int ammo, string gunDatas, Player player)
        {
            await PoliceSQLUtil.registerArmury(gun, tazer, ammo, gunDatas, player.character.Id);
        }

        public async void initDatabase()
        {
            await PoliceSQLUtil.Init(policeDirectoryPath);
        }
    }
}
