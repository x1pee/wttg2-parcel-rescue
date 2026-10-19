using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

[BepInPlugin("pmxp.wttg2.parcel-rescue", "Parcel Rescue", "1.0.0")]
public sealed class ParcelRescue : BaseUnityPlugin
{
    private const BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
    private ConfigEntry<bool> enabledSetting;
    private ConfigEntry<KeyCode> key;
    private ConfigEntry<float> radius;
    private FieldInfo products, product, interactionField;
    private MethodInfo pickup;
    private bool failed;
    private float nextUse, messageUntil;
    private string message;
    private void Awake()
    {
        enabledSetting=Config.Bind("General","Enabled",true,"Enable hotkey collection.");
        key=Config.Bind("General","Key",KeyCode.F6,"Collect the nearest delivered parcel while walking.");
        radius=Config.Bind("General","Radius",30f,"Collection distance in metres (1 to 200).");
        products=typeof(ProductsManager).GetField("currentShippedProducts",PrivateInstance);
        product=typeof(ShippedProductObject).GetField("myProduct",PrivateInstance);
        interactionField=typeof(ShippedProductObject).GetField("myInteractionHook",PrivateInstance);
        pickup=typeof(ShippedProductObject).GetMethod("pickUpPackage",PrivateInstance);
        failed=products==null || product==null || pickup==null || interactionField==null;
        if(failed) Logger.LogError("Unsupported game build; parcel collection disabled.");
        else Logger.LogInfo("Ready. Key="+key.Value+"; no game methods patched.");
    }
    private void Update()
    {
        if(failed || !enabledSetting.Value || !Application.isFocused || !Input.GetKeyDown(key.Value)) return;
        if(Time.realtimeSinceStartup<nextUse) return;
        nextUse=Time.realtimeSinceStartup+1.25f;
        if(StateManager.PlayerState!=PLAYER_STATE.ROAMING || Time.timeScale<=0f) {Show("Нужно встать и выйти из меню.");return;}
        Camera camera=Camera.main;
        if(camera==null) {Show("Камера игрока пока недоступна.");return;}
        CollectAt(camera.transform.position);
    }
    private bool Eligible(ShippedProductObject box, ProductsManager manager)
    {
        if(box==null || !box.gameObject.activeInHierarchy || box.transform.parent!=manager.ShippedProductParent) return false;
        MeshRenderer render=box.GetComponent<MeshRenderer>();
        MeshCollider collider=box.GetComponent<MeshCollider>();
        Rigidbody body=box.GetComponent<Rigidbody>();
        InteractionHook interaction=interactionField.GetValue(box) as InteractionHook;
        ShadowMarketProductDefinition definition=product.GetValue(box) as ShadowMarketProductDefinition;
        return render!=null && render.enabled && collider!=null && collider.enabled && body!=null && !body.isKinematic
            && interaction!=null && !interaction.ForceLock && definition!=null && definition.productIsShipped;
    }
    // Called only by the hotkey in production. QA calls this with a synthetic origin in an isolated save profile.
    private bool CollectAt(Vector3 origin)
    {
        if(failed || !enabledSetting.Value) return false;
        try {
            ProductsManager manager=GameManager.ManagerSlinger.ProductsManager;
            if(manager==null) {Show("Доставки пока недоступны.");return false;}
            List<ShippedProductObject> list=products.GetValue(manager) as List<ShippedProductObject>;
            ShippedProductObject nearest=null;
            float distance=Mathf.Clamp(radius.Value,1f,200f); distance*=distance;
            if(list!=null) foreach(ShippedProductObject box in list) {
                if(!Eligible(box,manager)) continue;
                float squared=(box.transform.position-origin).sqrMagnitude;
                if(squared<=distance) {distance=squared;nearest=box;}
            }
            if(nearest==null) {Show("Рядом нет доставленной посылки.");return false;}
            pickup.Invoke(nearest,null);
            Logger.LogInfo("Nearest delivered parcel collected using the original pickup handler.");
            Show("Посылка подобрана.");
            return true;
        } catch(Exception error) {
            failed=true; // Never retry a partially completed inventory operation.
            Logger.LogError("Collection disabled for this session: "+error);
            Show("Ошибка подбора. См. BepInEx/LogOutput.log.");
            return false;
        }
    }
    private void Show(string text) {message=text;messageUntil=Time.realtimeSinceStartup+4f;}
    private void OnGUI()
    {
        if(Time.realtimeSinceStartup>=messageUntil || string.IsNullOrEmpty(message)) return;
        GUI.Box(new Rect(20,20,520,42),message);
    }
}
