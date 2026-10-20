using UnityEngine;
using UnityEngine.UI;

namespace TreasureHunt
{
    // All graphics, labels and buttons are ordinary serialized Canvas children.
    public sealed class HuntUI : MonoBehaviour
    {
        public GameObject MainMenu, HUD, PauseMenu, Victory, GameOver;
        public Text Objective, HealthAndTime, Notice, VictoryTime, DefeatTime, DefeatReason, Seed;
        public Image DamageOverlay;
        public Graphic[] LifeHearts;
        public Text BestTimeLabel;
        public Text[] InventoryLabels;
        public Image[] InventoryIcons;
        int displayedState=-1;

        public void Refresh(HuntGame game)
        {
            if(displayedState!=game.State)
            {
                MainMenu.SetActive(game.State==0); HUD.SetActive(game.State==1);
                PauseMenu.SetActive(game.State==2); Victory.SetActive(game.State==3); GameOver.SetActive(game.State==4);
                displayedState=game.State;
            }
            Objective.text=game.KeysCollected<game.Maze.Keys.Count?"Find colored keys  "+game.KeysCollected+" / "+game.Maze.Keys.Count:game.DoorsOpened<game.Maze.Doors.Count?"Unlock the doors  "+game.DoorsOpened+" / "+game.Maze.Doors.Count:"Reach the hidden vault";
            if(InventoryLabels!=null) for(int i=0;i<InventoryLabels.Length;i++)
            {
                bool exists=i<game.Maze.Doors.Count, held=game.HasKeyOf((KeyKind)i);
                InventoryLabels[i].transform.parent.gameObject.SetActive(exists);
                InventoryLabels[i].text=held?((KeyKind)i).ToString().ToUpper()+" KEY":"EMPTY";
                InventoryLabels[i].color=held?HuntKey.ColorFor((KeyKind)i):new Color(.4f,.5f,.52f);
                InventoryIcons[i].color=held?HuntKey.ColorFor((KeyKind)i):new Color(.15f,.22f,.24f);
                foreach(var graphic in InventoryIcons[i].GetComponentsInChildren<Image>()) graphic.color=InventoryIcons[i].color;
            }
            HealthAndTime.text=HuntGame.Format(game.Elapsed);
            if(LifeHearts!=null) for(int i=0;i<LifeHearts.Length;i++) LifeHearts[i].color=i<game.Lives?new Color(1,.45f,.40f):new Color(.18f,.26f,.28f);
            Notice.text=game.Notice;
            VictoryTime.text=HuntGame.Format(game.Elapsed);
            if(BestTimeLabel!=null) BestTimeLabel.text="PERSONAL BEST  "+HuntGame.Format(game.BestTime);
            DefeatTime.text=HuntGame.Format(game.Elapsed);
            DefeatReason.text=game.Result;
            Seed.text="PROCEDURAL MAZE  /  SEED "+game.Settings.seed;
            Seed.transform.parent.gameObject.SetActive(game.State==1);
            DamageOverlay.color=new Color(1,.1f,.05f,game.State==1?game.DamageFlash*.5f:0);
        }
    }
}
