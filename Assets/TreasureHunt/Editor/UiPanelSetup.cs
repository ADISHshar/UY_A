using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace TreasureHunt.Editor
{
    public static class UiPanelSetup
    {
        static readonly Color Ink=new Color(.025f,.065f,.085f,1),Card=new Color(.03f,.09f,.11f,.98f),Cyan=new Color(.20f,.85f,.81f),Line=new Color(.08f,.28f,.31f),Muted=new Color(.48f,.68f,.72f),White=new Color(.82f,.92f,.94f),Gold=new Color(1,.78f,.32f),Coral=new Color(1,.45f,.4f);
        static Font Heading=>AssetDatabase.LoadAssetAtPath<Font>("Assets/TreasureHunt/UI/PanelHeading.ttf");
        static Font Mono=>AssetDatabase.LoadAssetAtPath<Font>("Assets/TreasureHunt/UI/PanelMono.ttf");
        static Font Body=>AssetDatabase.LoadAssetAtPath<Font>("Assets/TreasureHunt/UI/PanelBody.ttf");
        [MenuItem("Treasure Hunt/Apply Reference UI Panels")]
        public static void Apply()
        {
            var game=UnityEngine.Object.FindAnyObjectByType<HuntGame>();
            if(game==null || game.UI==null) throw new InvalidOperationException("Open the TreasureHunt scene first.");
            var ui=game.UI; var root=ui.transform;
            for(int i=root.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight=.5f;
            ui.MainMenu=Screen("Main Menu",root,"TREASURE\nHUNT","Collect colored keys. Outsmart the sentinels.",Gold);
            Controls(ui.MainMenu.transform.Find("Card"));
            Buttons(ui.MainMenu.transform.Find("Card"),"BEGIN EXPEDITION",game.Resume,"QUIT",game.QuitGame);
            ui.PauseMenu=Screen("Pause Menu",root,"EXPEDITION\nPAUSED","Take a breath. The ruins can wait.",Gold);
            Controls(ui.PauseMenu.transform.Find("Card"));
            Buttons(ui.PauseMenu.transform.Find("Card"),"RESUME",game.Resume,"MAIN MENU",game.MainMenu);
            ui.GameOver=Screen("Game Over Screen",root,"EXPEDITION\nLOST","Collect colored keys. Outsmart the sentinels.",Coral);
            var defeat=ui.GameOver.transform.Find("Card");
            ui.DefeatReason=defeat.Find("Description").GetComponent<Text>();
            ui.DefeatTime=ResultTime(defeat);
            Label("Tip",defeat,"Study the patrols. Time your next crossing.",64,426,630,30,20,Muted);
            Buttons(defeat,"TRY AGAIN",game.Restart,"MAIN MENU",game.MainMenu);
            ui.Victory=Screen("Victory Screen",root,"TREASURE\nRECOVERED","The lost treasure is yours.",Gold);
            var victory=ui.Victory.transform.Find("Card");
            ui.VictoryTime=ResultTime(victory);
            Label("Completion",victory,"Every key found. The vault is open.",64,426,630,30,20,Muted);
            ui.BestTimeLabel=Label("Personal Best",victory,"PERSONAL BEST  00:00.0",64,460,630,20,13,Muted,Mono);
            Buttons(victory,"TRY AGAIN",game.Restart,"MAIN MENU",game.MainMenu);
            ui.HUD=Full("HUD",root,false);
            var hud=ui.HUD.transform;
            var top=Box("Status Background",hud,32,28,1216,84,Card); Border(top,1216,84,Line);
            Label("Game Name",top,"T R E A S U R E  H U N T   /   T H E  S U N K E N  V A U L T",32,12,670,20,12,Cyan,Mono);
            Label("Objective Caption",top,"O B J E C T I V E",32,43,145,28,15,Muted,Mono);
            ui.Objective=Label("Objective",top,"Find colored keys",175,38,520,38,25,White);
            ui.LifeHearts=new Graphic[Mathf.Max(1,game.Settings.lives)];
            for(int i=0;i<ui.LifeHearts.Length;i++)
            {
                var heart=Rect("Life "+(i+1),top,735+i*36,31,24,24).gameObject.AddComponent<HuntGlyph>();
                heart.symbol=HuntGlyph.Symbol.Heart; heart.color=Coral; heart.raycastTarget=false; ui.LifeHearts[i]=heart;
            }
            ui.HealthAndTime=Label("Timer",top,"00:00.0",880,23,180,48,35,White,Mono);
            Label("Shortcuts",top,"ESC PAUSE\nSHIFT SPRINT",1065,20,140,45,12,Muted,Mono);
            var bar=Rect("Key Inventory",hud,0,0,666,74); bar.anchorMin=bar.anchorMax=new Vector2(.5f,0);bar.pivot=new Vector2(.5f,0);bar.anchoredPosition=new Vector2(0,32);
            var layout=bar.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.childAlignment=TextAnchor.MiddleCenter;layout.spacing=14;layout.childControlHeight=layout.childControlWidth=false;layout.childForceExpandHeight=layout.childForceExpandWidth=false;
            ui.InventoryLabels=new Text[5];ui.InventoryIcons=new Image[5];
            for(int i=0;i<5;i++)
            {
                var slot=Box(((KeyKind)i)+" Key Slot",bar,0,0,122,74,Card);Border(slot,122,74,Line);
                var key=Rect("Key Icon",slot,16,26,30,23).gameObject.AddComponent<HuntGlyph>();key.symbol=HuntGlyph.Symbol.Key;key.color=Muted;key.raycastTarget=false;ui.InventoryIcons[i]=key;
                ui.InventoryLabels[i]=Label("Held Item",slot,"EMPTY",54,28,66,26,12,Muted,Mono);
            }
            ui.Notice=Label("Notice",hud,"Walk near robots. Sprinting footsteps attract attention.",270,574,740,30,15,Muted);
            ui.Notice.alignment=TextAnchor.MiddleCenter;
            var footer=Rect("Shared Footer",root,16,694,1248,22);
            ui.Seed=Label("Seed",footer,"PROCEDURAL MAZE  /  SEED "+game.Settings.seed,0,0,760,20,12,Cyan,Mono);
            var credit=Label("Credit",footer,"B Y  A D I S H  S H A R M A",890,0,358,20,12,Cyan,Mono);credit.alignment=TextAnchor.MiddleRight;
            var overlay=Full("Damage Flash",root,false);ui.DamageOverlay=overlay.AddComponent<Image>();ui.DamageOverlay.color=Color.clear;ui.DamageOverlay.raycastTarget=false;ui.Seed.transform.parent.gameObject.SetActive(false);
            ui.MainMenu.SetActive(true);ui.HUD.SetActive(false);ui.PauseMenu.SetActive(false);ui.Victory.SetActive(false);ui.GameOver.SetActive(false);
            EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
            Debug.Log("REFERENCE UI SAVED: scalable cyan panels, gold/coral result cards, heart HUD, key inventory and working menu buttons.");
        }
        static GameObject Screen(string name,Transform root,string title,string description,Color accent)
        {
            var panel=Full(name,root,true);
            var grid=Full("Technical Grid",panel.transform,false).AddComponent<HuntGlyph>();grid.symbol=HuntGlyph.Symbol.Grid;grid.color=new Color(.08f,.24f,.27f,.35f);grid.raycastTarget=false;
            var card=Box("Card",panel.transform,264,64,752,612,Card);Border(card,752,612,Line);
            foreach(var corner in new[]{new Vector2(0,0),new Vector2(728,0),new Vector2(0,588),new Vector2(728,588)})
            {
                Box("Corner H",card,corner.x,corner.y+(corner.y>0?22:0),24,2,Cyan);
                Box("Corner V",card,corner.x+(corner.x>0?22:0),corner.y,2,24,Cyan);
            }
            Label("Eyebrow",card,"A N  E X P E D I T I O N  I N T O  T H E  F O R G O T T E N  R U I N S",64,44,640,28,12,Cyan,Mono);
            var heading=Label("Title",card,title,64,91,650,152,62,accent,Heading);heading.fontStyle=FontStyle.Bold;heading.lineSpacing=.95f;
            Label("Description",card,description,64,258,630,60,22,White);
            Box("Divider",card,64,324,624,1,Line);
            Label("Seed",card,"P R O C E D U R A L  M A Z E   /   S E E D  "+UnityEngine.Object.FindAnyObjectByType<HuntGame>().Settings.seed,64,578,624,20,12,Muted,Mono).alignment=TextAnchor.MiddleCenter;
            return panel;
        }
        static Text ResultTime(Transform card)
        {
            Label("Time Caption",card,"E X P E D I T I O N  T I M E",64,351,624,22,14,Muted,Mono);
            return Label("Time",card,"00:00.0",64,375,624,52,44,White,Mono);
        }
        static void Controls(Transform card)
        {
            var names=new[]{"M O V E","O R B I T","S P R I N T"};
            var values=new[]{"WASD / Arrows","Mouse","Shift"};
            for(int i=0;i<3;i++)
            {
                Label("Control "+names[i],card,names[i],64,350+i*35,110,28,14,Muted,Mono);
                Label("Control Value "+i,card,values[i],164,346+i*35,460,30,20,White);
            }
            Label("Tip",card,"Walk near robots. Step off clicking floors before they collapse.",64,460,625,28,17,Muted);
        }
        static void Buttons(Transform card,string primary,UnityAction primaryAction,string secondary,UnityAction secondaryAction)
        {
            Button("Primary Button",card,primary,64,510,216,true,primaryAction);
            Button("Secondary Button",card,secondary,300,510,232,false,secondaryAction);
        }
        static void Button(string name,Transform parent,string caption,float x,float y,float width,bool primary,UnityAction action)
        {
            var rect=Box(name,parent,x,y,width,62,primary?Cyan:Ink);if(!primary)Border(rect,width,62,Line);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();button.targetGraphic.raycastTarget=true;
            var colors=button.colors;colors.highlightedColor=new Color(.78f,1,1);colors.pressedColor=new Color(.45f,.75f,.76f);button.colors=colors;
            var text=Label("Label",rect,caption,8,0,width-16,62,20,primary?Ink:White,Heading);text.alignment=TextAnchor.MiddleCenter;text.fontStyle=FontStyle.Bold;
            UnityEventTools.AddPersistentListener(button.onClick,action);
        }
        static GameObject Full(string name,Transform parent,bool background)
        {
            var rect=Rect(name,parent,0,0,0,0);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            if(background) {var image=rect.gameObject.AddComponent<Image>();image.color=Ink;image.raycastTarget=false;}
            return rect.gameObject;
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
        }
        static RectTransform Box(string name,Transform parent,float x,float y,float w,float h,Color color)
        {
            var rect=Rect(name,parent,x,y,w,h);var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return rect;
        }
        static void Border(Transform rect,float w,float h,Color color)
        {
            Box("Top Border",rect,0,0,w,1,color);Box("Bottom Border",rect,0,h-1,w,1,color);Box("Left Border",rect,0,0,1,h,color);Box("Right Border",rect,w-1,0,1,h,color);
        }
        static Text Label(string name,Transform parent,string content,float x,float y,float w,float h,int size,Color color,Font font=null)
        {
            var label=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>();label.font=font!=null?font:Body;label.text=content;label.fontSize=size;label.color=color;label.raycastTarget=false;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;return label;
        }
    }
}
