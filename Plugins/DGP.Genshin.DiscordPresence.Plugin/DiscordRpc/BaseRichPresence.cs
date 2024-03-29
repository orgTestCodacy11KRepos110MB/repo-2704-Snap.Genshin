﻿using Newtonsoft.Json;
using System;
using System.Text;
/// <summary>
/// The base rich presence structure
/// </summary>
[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
[Serializable]
public class BaseRichPresence
{
    /// <summary>
    /// The user's current <see cref="Party"/> status. For example, "Playing Solo" or "With Friends".
    /// <para>Max 128 bytes</para>
    /// </summary>
    [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
    public string State
    {
        get { return _state; }
        set
        {
            if (!ValidateString(value, out _state, 128, Encoding.UTF8))
                throw new StringOutOfRangeException("State", 0, 128);
        }
    }

    /// <summary>Inernal inner state string</summary>
    protected internal string _state;

    /// <summary>
    /// What the user is currently doing. For example, "Competitive - Total Mayhem".
    /// <para>Max 128 bytes</para>
    /// </summary>
    [JsonProperty("details", NullValueHandling = NullValueHandling.Ignore)]
    public string Details
    {
        get { return _details; }
        set
        {
            if (!ValidateString(value, out _details, 128, Encoding.UTF8))
                throw new StringOutOfRangeException(128);
        }
    }
    /// <summary>Inernal inner detail string</summary>
    protected internal string _details;

    /// <summary>
    /// The time elapsed / remaining time data.
    /// </summary>
    [JsonProperty("timestamps", NullValueHandling = NullValueHandling.Ignore)]
    public Timestamps Timestamps { get; set; }

    /// <summary>
    /// The names of the images to use and the tooltips to give those images.
    /// </summary>
    [JsonProperty("assets", NullValueHandling = NullValueHandling.Ignore)]
    public Assets Assets { get; set; }

    /// <summary>
    /// The party the player is currently in. The <see cref="Party.ID"/> must be set for this to be included in the RichPresence update.
    /// </summary>
    [JsonProperty("party", NullValueHandling = NullValueHandling.Ignore)]
    public Party Party { get; set; }

    /// <summary>
    /// The secrets used for Join / Spectate. Secrets are obfuscated data of your choosing. They could be match ids, player ids, lobby ids, etc. Make this object null if you do not wish too / unable too implement the Join / Request feature.
    /// <para>To keep security on the up and up, Discord requires that you properly hash/encode/encrypt/put-a-padlock-on-and-swallow-the-key-but-wait-then-how-would-you-open-it your secrets.</para>
    /// <para>Visit the <see href="https://discordapp.com/developers/docs/rich-presence/how-to#secrets">Rich Presence How-To</see> for more information.</para>
    /// </summary>
    [JsonProperty("secrets", NullValueHandling = NullValueHandling.Ignore)]
    public Secrets Secrets { get; set; }

    /// <summary>
    /// Marks the <see cref="Secrets.MatchSecret"/> as a game session with a specific beginning and end. It was going to be used as a form of notification, but was replaced with the join feature. It may potentially have use in the future, but it currently has no use.
    /// <para>
    /// "TLDR it marks the matchSecret field as an instance, that is to say a context in game that’s not like a lobby state/not in game state. It was gonna he used for notify me, but we scrapped that for ask to join. We may put it to another use in the future. For now, don’t worry about it" - Mason (Discord API Server 14 / 03 / 2018)
    ///    </para>
    /// </summary>
    [JsonProperty("instance", NullValueHandling = NullValueHandling.Ignore)]
    [Obsolete("This was going to be used, but was replaced by JoinSecret instead")]
    private bool Instance { get; set; }

    #region Has Checks
    /// <summary>
    /// Does the Rich Presence have valid timestamps?
    /// </summary>
    /// <returns></returns>
    public bool HasTimestamps()
    {
        return this.Timestamps != null && (Timestamps.Start != null || Timestamps.End != null);
    }

    /// <summary>
    /// Does the Rich Presence have valid assets?
    /// </summary>
    /// <returns></returns>
    public bool HasAssets()
    {
        return this.Assets != null;
    }

    /// <summary>
    /// Does the Rich Presence have a valid party?
    /// </summary>
    /// <returns></returns>
    public bool HasParty()
    {
        return this.Party != null && this.Party.ID != null;
    }

    /// <summary>
    /// Does the Rich Presence have valid secrets?
    /// </summary>
    /// <returns></returns>
    public bool HasSecrets()
    {
        return Secrets != null && (Secrets.JoinSecret != null || Secrets.SpectateSecret != null);
    }

    #endregion


    /// <summary>
    /// Attempts to call <see cref="StringTools.GetNullOrString(string)"/> on the string and return the result, if its within a valid length.
    /// </summary>
    /// <param name="str">The string to check</param>
    /// <param name="result">The formatted string result</param>
    /// <param name="bytes">The maximum number of bytes the string can take up</param>
    /// <param name="encoding">The encoding to count the bytes with</param>
    /// <returns>True if the string fits within the number of bytes</returns>
    internal static bool ValidateString(string str, out string result, int bytes, Encoding encoding)
    {
        result = str;
        if (str == null)
            return true;

        //Trim the string, for the best chance of fitting
        var s = str.Trim();

        //Make sure it fits
        if (!s.WithinLength(bytes, encoding))
            return false;

        //Make sure its not empty
        result = s.GetNullOrString();
        return true;
    }

    /// <summary>
    /// Operator that converts a presence into a boolean for null checks.
    /// </summary>
    /// <param name="presesnce"></param>
    public static implicit operator bool(BaseRichPresence presesnce)
    {
        return presesnce != null;
    }

    /// <summary>
    /// Checks if the other rich presence differs from the current one
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    internal virtual bool Matches(RichPresence other)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        if (other == null)
            return false;

        if (State != other.State || Details != other.Details)
            return false;

        //Checks if the timestamps are different
        if (Timestamps != null)
        {
            if (other.Timestamps == null ||
                other.Timestamps.StartUnixMilliseconds != Timestamps.StartUnixMilliseconds ||
                other.Timestamps.EndUnixMilliseconds != Timestamps.EndUnixMilliseconds)
                return false;
        }
        else if (other.Timestamps != null)
        {
            return false;
        }

        //Checks if the secrets are different
        if (Secrets != null)
        {
            if (other.Secrets == null ||
                other.Secrets.JoinSecret != Secrets.JoinSecret ||
                other.Secrets.MatchSecret != Secrets.MatchSecret ||
                other.Secrets.SpectateSecret != Secrets.SpectateSecret)
                return false;
        }
        else if (other.Secrets != null)
        {
            return false;
        }

        //Checks if the timestamps are different
        if (Party != null)
        {
            if (other.Party == null ||
                other.Party.ID != Party.ID ||
                other.Party.Max != Party.Max ||
                other.Party.Size != Party.Size ||
                other.Party.Privacy != Party.Privacy)
                return false;
        }
        else if (other.Party != null)
        {
            return false;
        }

        //Checks if the assets are different
        if (Assets != null)
        {
            if (other.Assets == null ||
                other.Assets.LargeImageKey != Assets.LargeImageKey ||
                other.Assets.LargeImageText != Assets.LargeImageText ||
                other.Assets.SmallImageKey != Assets.SmallImageKey ||
                other.Assets.SmallImageText != Assets.SmallImageText)
                return false;
        }
        else if (other.Assets != null)
        {
            return false;
        }

        return Instance == other.Instance;
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Converts this BaseRichPresence to RichPresence
    /// </summary>
    /// <returns></returns>
    public RichPresence ToRichPresence()
    {
        var presence = new RichPresence();
        presence.State = State;
        presence.Details = Details;

        presence.Party = !HasParty() ? Party : null;
        presence.Secrets = !HasSecrets() ? Secrets : null;

        if (HasAssets())
        {
            presence.Assets = new Assets()
            {
                SmallImageKey = Assets.SmallImageKey,
                SmallImageText = Assets.SmallImageText,

                LargeImageKey = Assets.LargeImageKey,
                LargeImageText = Assets.LargeImageText
            };
        }

        if (HasTimestamps())
        {
            presence.Timestamps = new Timestamps();
            if (Timestamps.Start.HasValue) presence.Timestamps.Start = Timestamps.Start;
            if (Timestamps.End.HasValue) presence.Timestamps.End = Timestamps.End;
        }

        return presence;
    }
}
