﻿using DGP.Snap.Framework.Attributes.DataModel;
using Newtonsoft.Json;

namespace DGP.Genshin.Models.MiHoYo.UserInfo
{
    [JsonModel]
    public class UserFuncStatus
    {
        [JsonProperty("enable_history_view")] public bool EnableHistoryView { get; set; }
        [JsonProperty("enable_recommend")] public bool EnableRecommend { get; set; }
        [JsonProperty("enable_mention")] public bool EnableMention { get; set; }
    }
}