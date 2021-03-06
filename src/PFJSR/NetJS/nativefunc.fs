﻿namespace PFJSR

open System.IO
open System.Threading
open System.Threading.Tasks
open API
open CSR
open type Newtonsoft.Json.JsonConvert
open Newtonsoft.Json.Linq

module NativeFunc=
    module Basic=
        let shares  = new System.Collections.Generic.Dictionary<string,obj>()
        type log_delegate = delegate of string -> unit
        let log=log_delegate(fun e-> Console.log(e))
        type fileReadAllText_delegate = delegate of string -> string
        let fileReadAllText=
            fileReadAllText_delegate (fun e->  
                try
                    e|>File.ReadAllText 
                with _ -> null
                )
        type fileWriteAllText_delegate = delegate of string*string -> bool
        let fileWriteAllText=
            fileWriteAllText_delegate(fun f c ->
                try
                    (f,c)|>File.WriteAllText
                    true
                with _ -> false
                )
        type fileWriteLine_delegate = delegate of string*string-> bool
        let fileWriteLine=
            fileWriteLine_delegate(fun f c ->
                try
                    (f,[c])|>File.AppendAllLines
                    true
                with _ -> false
                )
        type TimeNow_delegate = delegate of unit -> string
        let TimeNow=
            TimeNow_delegate(fun _ ->
                System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                )
        type setShareData_delegate = delegate of string*obj-> unit
        let setShareData=
            setShareData_delegate(fun k o ->
                    if shares.ContainsKey(k) then shares.[k] <- o else shares.Add(k,o)
                )
        type getShareData_delegate = delegate of string -> obj
        let getShareData=
            getShareData_delegate(fun k ->
                    if shares.ContainsKey(k) then shares.[k] else Jint.Native.Undefined.Instance:>obj
                    )
        type removeShareData_delegate = delegate of string -> obj
        let removeShareData=
            removeShareData_delegate(fun k->
                    if shares.ContainsKey(k) then 
                        let o=shares.[k]
                        k|> shares.Remove|>ignore
                        o
                    else
                        Jint.Native.Undefined.Instance:>obj
                )
        type mkdir_delegate = delegate of string-> bool
        let mkdir=
            mkdir_delegate(fun dirname ->
                    let mutable dir :DirectoryInfo= null;
                    if not (dirname|>isNull) then
                        try
                            dir <- Directory.CreateDirectory(dirname)
                        with _->()
                    not (dir|>isNull)
                )
        type getWorkingPath_delegate = delegate of unit-> string
        let getWorkingPath=
            getWorkingPath_delegate(fun _ ->
                    System.AppDomain.CurrentDomain.BaseDirectory
                )
    module Core =
        type addBeforeActListener_delegate = delegate of string*System.Func<string,obj> -> int
        type addAfterActListener_delegate = delegate of string*System.Func<string,obj> -> int
        type removeBeforeActListener_delegate = delegate of string*int -> unit
        type removeAfterActListener_delegate = delegate of string*int -> unit
        type setCommandDescribe_delegate = delegate of string*string -> unit
        type runcmd_delegate = delegate of string -> bool
        type logout_delegate = delegate of string -> unit
        type getOnLinePlayers_delegate = delegate of unit -> string
        type getStructure_delegate = delegate of int*string*string*bool*bool -> string
        type setStructure_delegate = delegate of string*int*string*byte*bool*bool -> bool
        type setServerMotd_delegate = delegate of string*bool -> bool
        type JSErunScript_delegate = delegate of string*System.Action<bool> -> unit
        type JSEfireCustomEvent_delegate = delegate of string*string*System.Action<bool> -> unit
        type reNameByUuid_delegate = delegate of string*string -> bool
        type getPlayerAbilities_delegate = delegate of string -> string
        type setPlayerAbilities_delegate = delegate of string*string -> bool
        type getPlayerAttributes_delegate = delegate of string -> string
        type setPlayerTempAttributes_delegate = delegate of string*string -> bool
        type getPlayerMaxAttributes_delegate = delegate of string -> string
        type setPlayerMaxAttributes_delegate = delegate of string*string -> bool
        type getPlayerItems_delegate = delegate of string -> string
        type getPlayerSelectedItem_delegate = delegate of string -> string
        type setPlayerItems_delegate = delegate of string*string -> bool
        type addPlayerItemEx_delegate = delegate of string*string -> bool
        type addPlayerItem_delegate = delegate of string*int*int16*byte -> bool
        type getPlayerEffects_delegate = delegate of string -> string
        type setPlayerEffects_delegate = delegate of string*string -> bool
        type setPlayerBossBar_delegate = delegate of string*string*float32 -> bool
        type removePlayerBossBar_delegate = delegate of string -> bool
        type selectPlayer_delegate = delegate of string -> string
        type transferserver_delegate = delegate of string*string*int -> bool
        type teleport_delegate = delegate of string*float32*float32*float32*int -> bool
        type talkAs_delegate = delegate of string*string -> bool
        type runcmdAs_delegate = delegate of string*string -> bool
        type sendSimpleForm_delegate = delegate of string*string*string*string -> uint
        type sendModalForm_delegate = delegate of string*string*string*string*string -> uint
        type sendCustomForm_delegate = delegate of string*string -> uint
        type releaseForm_delegate = delegate of uint -> bool
        type setPlayerSidebar_delegate = delegate of string*string*string -> bool
        type removePlayerSidebar_delegate = delegate of string -> bool
        type getPlayerPermissionAndGametype_delegate = delegate of string -> string
        type setPlayerPermissionAndGametype_delegate = delegate of string*string -> bool
        type disconnectClient_delegate = delegate of string*string -> bool
        type sendText_delegate = delegate of string*string -> bool
        type getscoreboard_delegate = delegate of string*string -> int
        type setscoreboard_delegate = delegate of string*string*int -> bool
        type getPlayerIP_delegate = delegate of string -> string
        type request_delegate = delegate of string*string*string*System.Action<obj> -> unit
        type setTimeout_delegate = delegate of obj*int -> unit
        type runScript_delegate = delegate of obj -> unit
        type Instance(scriptName:string,engine:Jint.Engine) =
            let CheckUuid(uuid:string )=
                if System.String.IsNullOrWhiteSpace(uuid) then
                    let funcname = (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name
                    let err = $"在脚本\"{scriptName}\"调用\"{funcname}\"函数时使用了空的uuid！"
                    err|>Console.WriteLine
                    err|>failwith 
                if Data.Config.JSR.CheckUuid then
                    if (uuid,"^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$")|>System.Text.RegularExpressions.Regex.IsMatch|>not then
                        let funcname = (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name
                        let err = $"在脚本\"{scriptName}\"调用\"{funcname}\"函数时使用了无效的uuid:\"{uuid}\"！"
                        err|>Console.WriteLine
                        err|>failwith
            let AssertCommercial()=
                if not api.COMMERCIAL then
                    let fn = (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name
                    let err = $"获取方法\"{fn}\"失败，社区版不支持该方法！"
                    err|>Console.WriteLine
                    err|>failwith 
            let InvokeRemoveFailed(a1:string ,a2:string)= 
                ("在脚本\""+scriptName+"\"执行\""+a1+"\"无效",new exn( "参数2的值仅可以通过\""+a2+"\"结果获得"))|>Console.WriteLineErr
            let _BeforeActListeners =new System.Collections.Generic.List<(int*string*MCCSAPI.EventCab)>()
            let _AfterActListeners =new System.Collections.Generic.List<(int*string*MCCSAPI.EventCab)>()
            member _this.BeforeActListeners with get()=_BeforeActListeners
            member _this.AfterActListeners with get()=_AfterActListeners
            member _this.setTimeout=
                setTimeout_delegate(fun o ms->
                        if not (o|>isNull) then
                            Task.Run(fun _->
                            (
                                try
                                    ms|>Thread.Sleep
                                    if o.GetType()=typeof<string> then
                                        engine.Execute(o:?>string)|>ignore
                                    else
                                        (engine.ClrTypeConverter.Convert(o,typeof<System.Action>,null):?>System.Action).Invoke()
                                with ex->
                                (
                                    ($"在脚本\"{scriptName}\"执行\"setTimeout时遇到错误：",ex)|>Console.WriteLineErr
                                ) 
                            ))|>ignore
                    )
            member _this.runScript=
                runScript_delegate(fun o->
                        if not (o|>isNull) then
                                try
                                    if o.GetType()=typeof<string> then
                                        engine.Execute(o:?>string)|>ignore
                                    else
                                        (engine.ClrTypeConverter.Convert(o,typeof<System.Action>,null):?>System.Action).Invoke()
                                with ex->
                                (
                                    ($"在脚本\"{scriptName}\"执行\"runScript时遇到错误：",ex)|>Console.WriteLineErr
                    )
                )
            member _this.request=
                request_delegate(fun u m p f->
                        Task.Run(fun ()-> 
                            (
                                try
                                    let mutable ret:string = null;
                                    try
                                         ret <- PFJSRBDSAPI.Ex.Localrequest(u, m, p)
                                    with _-> ()
                                    if f|>isNull|>not then
                                        try
                                            ret|>f.Invoke
                                        with ex->
                                        (
                                           ($"在脚本\"{scriptName}\"执行\"[request]回调时遇到错误：",ex)|>Console.WriteLineErr
                                        ) 
                                with _-> ()
                            )
                            )|>ignore
                    )
            member _this.setBeforeActListener=_this.addBeforeActListener
            member _this.addBeforeActListener=
                addBeforeActListener_delegate(fun k f-> 
                (
                    let fullFunc=MCCSAPI.EventCab(fun e->
                        (
                            try
                                e|>BaseEvent.getFrom|>SerializeObject|>f.Invoke|>false.Equals|>not
                                //let got=e|>BaseEvent.getFrom
                                //let e= (got|>Newtonsoft.Json.Linq.JObject.FromObject)
                                //e.Add("result",new JValue( got.RESULT):>JToken)
                                //e.ToString()|>f.Invoke|>false.Equals|>not
                            with ex->
                            (
                                try
                                ("在脚本\""+scriptName+"\"执行\""+(int e.``type``|>enum<EventType>).ToString()+"\"BeforeAct回调时遇到错误：",ex)|>Console.WriteLineErr
                                with _->()
                                true
                            )
                        ))
                    let funcHash=f.Method.GetHashCode()
                    _this.BeforeActListeners.Add(funcHash,k,fullFunc)
                    (k,fullFunc)|>api.addBeforeActListener|>ignore
                    funcHash
                ))      
            member _this.setAfterActListener=_this.addAfterActListener
            member _this.addAfterActListener=
                addAfterActListener_delegate(fun k f-> 
                (
                    let fullFunc=MCCSAPI.EventCab(fun e->
                        (
                            try
                                let got=e|>BaseEvent.getFrom
                                let e= (got|>Newtonsoft.Json.Linq.JObject.FromObject)
                                e.Add("result",new JValue( got.RESULT):>JToken)
                                e.ToString(Newtonsoft.Json.Formatting.None)|>f.Invoke|>false.Equals|>not
                            with ex->
                            (
                                try
                                ("在脚本\""+scriptName+"\"执行\""+(int e.``type``|>enum<EventType>).ToString()+"\"AfterAct回调时遇到错误：",ex)|>Console.WriteLineErr
                                with _->()
                                true
                            )
                        ))
                    let funcHash=f.Method.GetHashCode()
                    _this.AfterActListeners.Add(funcHash,k,fullFunc)
                    (k,fullFunc)|>api.addAfterActListener|>ignore
                    funcHash
                ))  
            member this.removeBeforeActListener=
                removeBeforeActListener_delegate(fun k fhash-> 
                (   
                    try
                        let index=this.BeforeActListeners.FindIndex(fun (hash,_,_)->hash=fhash)
                        if index <> -1 then
                            let item=this.BeforeActListeners.[index]
                            let (_,_,getFunc)=item
                            (k, getFunc )|>api.removeBeforeActListener|>ignore
                            this.BeforeActListeners.Remove(item)|>ignore
                        else
                            InvokeRemoveFailed(nameof(this.removeBeforeActListener),nameof(this.addBeforeActListener))
                    with _-> InvokeRemoveFailed(nameof(this.removeBeforeActListener),nameof(this.addBeforeActListener))
              ))   
            member this.removeAfterActListener=
                removeAfterActListener_delegate(fun k fhash-> 
                (   
                    try
                         let index=this.AfterActListeners.FindIndex(fun (hash,_,_)->hash=fhash)
                         if index <> -1 then
                                let item=this.AfterActListeners.[index]
                                let (_,_,getFunc)=item
                                (k, getFunc )|>api.removeAfterActListener|>ignore
                                this.AfterActListeners.Remove(item)|>ignore
                         else
                                InvokeRemoveFailed(nameof(this.removeAfterActListener),nameof(this.addAfterActListener))
                    with _->InvokeRemoveFailed(nameof(this.removeAfterActListener),nameof(this.addAfterActListener))
                ))
            member _this.setCommandDescribe=setCommandDescribe_delegate(fun c s->(c,s)|>api.setCommandDescribe)
            member _this.runcmd=runcmd_delegate(fun cmd->cmd|>api.runcmd)
            member _this.logout=logout_delegate(fun l->l|>api.logout)
            member _this.getOnLinePlayers=getOnLinePlayers_delegate(fun ()->
                (
                    let result=api.getOnLinePlayers()
                    if result|>System.String.IsNullOrEmpty then "[]" else result
                ))
            member this.getStructure =getStructure_delegate(fun did posa posb exent exblk->
                (
                    AssertCommercial()
                    (did, posa, posb, exent, exblk)|>api.getStructure
                ))
            member this.setStructure =setStructure_delegate(fun jdata did jsonposa rot exent exblk->
                (
                    AssertCommercial()
                    (jdata, did, jsonposa, rot, exent, exblk)|>api.setStructure
                ))
            member _this.setServerMotd=setServerMotd_delegate(fun motd isShow->(motd, isShow)|>api.setServerMotd)
            member _this.JSErunScript=JSErunScript_delegate(fun js cb->
                (
                    let fullFunc=MCCSAPI.JSECab(fun result->
                        (
                        try
                            cb.Invoke(result)
                        with ex->
                            (
                            try
                            ($"在脚本\"{scriptName}\"执行\"JSErunScript回调时遇到错误：",ex)|>Console.WriteLineErr
                            with _->()
                            )
                        ))
                    (js,fullFunc)|>api.JSErunScript
                )
            )
            member _this.JSEfireCustomEvent=JSEfireCustomEvent_delegate(fun ename jdata cb->
                (
                    let fullFunc=MCCSAPI.JSECab(fun result->
                        (
                        try
                            cb.Invoke(result)
                        with ex->
                            (
                            try
                            ($"在脚本\"{scriptName}\"执行\"JSErunScript回调时遇到错误：",ex)|>Console.WriteLineErr
                            with _->()
                            )
                        ))
                    (ename, jdata,fullFunc)|>api.JSEfireCustomEvent
                )
            )
            member _this.reNameByUuid=reNameByUuid_delegate(fun uuid name->uuid|>CheckUuid;(uuid,name)|>api.reNameByUuid)
            member this.getPlayerAbilities =getPlayerAbilities_delegate(fun uuid->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    uuid|>api.getPlayerAbilities
                ))
            member this.setPlayerAbilities =setPlayerAbilities_delegate(fun uuid a->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    (uuid,a)|>api.setPlayerAbilities
                ))
            member this.getPlayerAttributes =getPlayerAttributes_delegate(fun uuid->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    uuid|>api.getPlayerAttributes
                ))
            member this.setPlayerTempAttributes =setPlayerTempAttributes_delegate(fun uuid a->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    (uuid,a)|>api.setPlayerTempAttributes
                ))
            member this.getPlayerMaxAttributes =getPlayerMaxAttributes_delegate(fun uuid->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    uuid|>api.getPlayerMaxAttributes
                ))
            member this.setPlayerMaxAttributes =setPlayerMaxAttributes_delegate(fun uuid a->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    (uuid,a)|>api.setPlayerMaxAttributes
                ))
            member this.getPlayerItems =getPlayerItems_delegate(fun uuid->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    uuid|>api.getPlayerItems
                ))
            member this.getPlayerSelectedItem =getPlayerSelectedItem_delegate(fun uuid->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    uuid|>api.getPlayerSelectedItem
                ))
            member this.setPlayerItems =setPlayerItems_delegate(fun uuid a->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    (uuid,a)|>api.setPlayerItems
                ))
            member this.addPlayerItemEx =addPlayerItemEx_delegate(fun uuid a->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    (uuid,a)|>api.addPlayerItemEx
                ))
            member _this.addPlayerItem =addPlayerItem_delegate(fun uuid id aux count->uuid|>CheckUuid;(uuid,id,aux,count)|>api.addPlayerItem)
            member this.getPlayerEffects =getPlayerEffects_delegate(fun uuid->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    uuid|>api.getPlayerEffects
                ))
            member this.setPlayerEffects=setPlayerEffects_delegate(fun uuid a->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    (uuid,a)|>api.setPlayerEffects
                ))
            member this.setPlayerBossBar=setPlayerBossBar_delegate(fun uuid title percent->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    (uuid,title,percent)|>api.setPlayerBossBar
                ))
            member this.removePlayerBossBar=removePlayerBossBar_delegate(fun uuid->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    uuid|>api.removePlayerBossBar
                ))
            member _this.selectPlayer=selectPlayer_delegate(fun uuid->uuid|>api.selectPlayer)
            member this.transferserver=transferserver_delegate(fun uuid addr port->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    (uuid, addr, port)|>api.transferserver
                ))
            member this.teleport=teleport_delegate(fun uuid x y z did->
                (
                    uuid|>CheckUuid
                    //try
                    //    {
                    //        const string key = ;
                    //        IntPtr ptr = CsApi.getSharePtr(key);
                    //        if (ptr == IntPtr.Zero) { GetPFEssentialsApiFailedTips(key); }
                    //        else
                    //        {
                    //            Action<string,string> org = (Action<string,string>)System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(ptr);
                    //            org.Invoke(name,cmd);
                    //        }
                    //}
                    //catch { }
                    if  api.COMMERCIAL then
                        (uuid,x,y,z,did)|>api.teleport
                    else
                        let ptr:System.IntPtr=api.getSharePtr("PFEssentials.PublicApi.V2.Teleport") 
                        let mutable result=false
                        if ptr <> System.IntPtr.Zero then
                            let org = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(ptr):?>System.Func<string,single,single,single,int,bool>
                            result<- ((uuid,x,y,z,did)|>org.Invoke )
                        if result|>not then
                            AssertCommercial()
                            (uuid,x,y,z,did)|>api.teleport
                        else result
                ))
            member _this.talkAs=talkAs_delegate(fun uuid a->uuid|>CheckUuid;(uuid,a)|>api.talkAs)
            member _this.runcmdAs=runcmdAs_delegate(fun uuid a->uuid|>CheckUuid;(uuid,a)|>api.runcmdAs)
            member _this.sendSimpleForm=sendSimpleForm_delegate(fun uuid title content buttons->uuid|>CheckUuid;(uuid,title,content,buttons)|>api.sendSimpleForm)
             member _this.sendModalForm=sendModalForm_delegate(fun uuid title content button1 button2->uuid|>CheckUuid;(uuid,title,content,button1,button2)|>api.sendModalForm)
            member _this.sendCustomForm=sendCustomForm_delegate(fun uuid json->uuid|>CheckUuid;(uuid,json)|>api.sendCustomForm)
            member _this.releaseForm=releaseForm_delegate(fun formid->formid|>api.releaseForm)
            member this.setPlayerSidebar=setPlayerSidebar_delegate(fun uuid title list->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    (uuid,title,list)|>api.setPlayerSidebar
                ))
            member this.removePlayerSidebar=removePlayerSidebar_delegate(fun uuid->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    uuid|>api.removePlayerSidebar
                ))
            member this.getPlayerPermissionAndGametype =getPlayerPermissionAndGametype_delegate(fun uuid->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    uuid|>api.getPlayerPermissionAndGametype
                ))
            member this.setPlayerPermissionAndGametype=setPlayerPermissionAndGametype_delegate(fun uuid a->
                (
                    uuid|>CheckUuid;AssertCommercial()
                    (uuid,a)|>api.setPlayerPermissionAndGametype
                ))
            member _this.disconnectClient=disconnectClient_delegate(fun uuid a->uuid|>CheckUuid;(uuid,a)|>api.disconnectClient)
            member _this.sendText=sendText_delegate(fun uuid a->
                CheckUuid(uuid)
                (uuid,a)|>api.sendText
            )
            member _this.getscoreboard=getscoreboard_delegate(fun uuid a->uuid|>CheckUuid;(uuid,a)|>api.getscoreboard)
            member _this.setscoreboard=setscoreboard_delegate(fun uuid sname value->uuid|>CheckUuid;(uuid,sname,value)|>api.setscoreboard)
            member _this.getPlayerIP=getPlayerIP_delegate(fun uuid->
                (
                    uuid|>CheckUuid;
                    let mutable result=System.String.Empty
                    let data = api.selectPlayer(uuid)
                    if data|>System.String.IsNullOrEmpty|>not then
                        let pinfo=Newtonsoft.Json.Linq.JObject.Parse(data)
                        if pinfo.ContainsKey("playerptr") then
                            let mutable ptr = pinfo.["playerptr"]|>System.Convert.ToInt64|>System.IntPtr
                            if ptr <> System.IntPtr.Zero then
                                let ipport=(new CsPlayer(api, ptr)).IpPort
                                Console.WriteLine(ipport)
                                result<-ipport.Substring(0, ipport.IndexOf('|'))
                    result
                ))

