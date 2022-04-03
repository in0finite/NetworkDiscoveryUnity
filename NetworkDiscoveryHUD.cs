using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Net;

namespace NetworkDiscoveryUnity
{
	
    public class NetworkDiscoveryHUD : MonoBehaviour
    {
        NetworkDiscovery m_networkDiscovery;

        readonly List<NetworkDiscovery.DiscoveryInfo> m_discoveredServers = new List<NetworkDiscovery.DiscoveryInfo>();

        public List<string> additionalDataToDisplay = new List<string>()
        {
            NetworkDiscovery.kMapNameKey,
        };

        Vector2 m_scrollViewPos = Vector2.zero;

        public bool IsRefreshing { get { return Time.realtimeSinceStartup - m_timeWhenRefreshed < this.refreshInterval; } }
        float m_timeWhenRefreshed = 0f;

        bool m_displayBroadcastAddresses = false;

        IPEndPoint m_lookupServer = null;   // server that we are currently looking up
        string m_lookupServerIP = "";
        string m_lookupServerPort = NetworkDiscovery.kDefaultServerPort.ToString();
        float m_timeWhenLookedUpServer = 0f;
        bool IsLookingUpAnyServer { get { return Time.realtimeSinceStartup - m_timeWhenLookedUpServer < this.refreshInterval
                                            && m_lookupServer != null; } }

        GUIStyle m_centeredLabelStyle;

        public bool drawGUI = true;
        public int offsetX = 5;
        public int offsetY = 150;
        public int width = 500, height = 400;
        [Range(1, 5)] public float refreshInterval = 3f;

        public UnityEngine.Events.UnityEvent<NetworkDiscovery.DiscoveryInfo> onConnectEvent
            = new UnityEngine.Events.UnityEvent<NetworkDiscovery.DiscoveryInfo>();


        void Awake()
        {
            m_networkDiscovery = this.GetComponent<NetworkDiscovery>();
        }

        void OnEnable()
        {
            m_networkDiscovery.onReceivedServerResponse.AddListener(OnDiscoveredServer);
        }

        void OnDisable()
        {
            m_networkDiscovery.onReceivedServerResponse.RemoveListener(OnDiscoveredServer);
        }

        void OnGUI()
        {
            
            if (null == m_centeredLabelStyle)
            {
                m_centeredLabelStyle = new GUIStyle(GUI.skin.label);
                m_centeredLabelStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (this.drawGUI)
                this.Display(new Rect(offsetX, offsetY, width, height));
            
        }

        public void Display(Rect displayRect)
        {
            if (!NetworkDiscovery.SupportedOnThisPlatform)
                return;

            GUILayout.BeginArea(displayRect);

            this.DisplayRefreshButton();

            // lookup a server

            GUILayout.Label("Lookup server: ");
            GUILayout.BeginHorizontal();
            GUILayout.Label("IP:");
            m_lookupServerIP = GUILayout.TextField(m_lookupServerIP, GUILayout.Width(120));
            GUILayout.Space(10);
            GUILayout.Label("Port:");
            m_lookupServerPort = GUILayout.TextField(m_lookupServerPort, GUILayout.Width(60));
            GUILayout.Space(10);
            if (IsLookingUpAnyServer)
            {
                GUILayout.Button("Lookup...", GUILayout.Height(25), GUILayout.MinWidth(80));
            }
            else
            {
                if (GUILayout.Button("Lookup", GUILayout.Height(25), GUILayout.MinWidth(80)))
                    LookupServer();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_displayBroadcastAddresses = GUILayout.Toggle(m_displayBroadcastAddresses, "Display broadcast addresses", GUILayout.ExpandWidth(false));
            if (m_displayBroadcastAddresses)
            {
                GUILayout.Space(10);
                GUILayout.Label( string.Join( ", ", NetworkDiscovery.GetBroadcastAdresses().Select(ip => ip.ToString()) ) );
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(string.Format("Servers [{0}]:", m_discoveredServers.Count));

            this.DisplayServers();

            GUILayout.EndArea();

        }

        public void DisplayRefreshButton()
        {
            if(IsRefreshing)
            {
                GUILayout.Button("Refreshing...", GUILayout.Height(25), GUILayout.ExpandWidth(false));
            }
            else
            {
                if (GUILayout.Button("Refresh LAN", GUILayout.Height(25), GUILayout.ExpandWidth(false)))
                {
                    Refresh();
                }
            }
        }

        public void DisplayServers()
        {

            var headerNames = Enumerable.Empty<string>().Append("IP").Concat(this.additionalDataToDisplay);

            int elemWidth = this.width / headerNames.Count() - 5;

            // header
            GUILayout.BeginHorizontal();
            foreach(string str in headerNames)
                GUILayout.Button(str, GUILayout.Width(elemWidth));
            GUILayout.EndHorizontal();

            // servers

            m_scrollViewPos = GUILayout.BeginScrollView(m_scrollViewPos);

            foreach(var info in m_discoveredServers)
            {
                GUILayout.BeginHorizontal();

                bool hasGameServerPort = info.TryGetGameServerPort(out ushort gameServerPort);

                if( GUILayout.Button(info.EndPoint.Address.ToString() + (hasGameServerPort ? $":{gameServerPort}" : ""), GUILayout.Width(elemWidth)) )
                    this.onConnectEvent.Invoke(info);

                foreach(string headerName in headerNames.Skip(1))
                {
                    GUILayout.Label(
                        info.KeyValuePairs.TryGetValue(headerName, out string value) ? value : "",
                        m_centeredLabelStyle,
                        GUILayout.Width(elemWidth));
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

        }

        public void Refresh()
        {
            m_discoveredServers.Clear();

            m_timeWhenRefreshed = Time.realtimeSinceStartup;

            m_networkDiscovery.SendBroadcast();
            
        }

        public void LookupServer()
        {
            // parse IP and port

            IPAddress ip = IPAddress.Parse(m_lookupServerIP);
            ushort port = ushort.Parse(m_lookupServerPort);

            // input is ok
            // send discovery request

            m_timeWhenLookedUpServer = Time.realtimeSinceStartup;

            m_lookupServer = new IPEndPoint(ip, port);

            m_networkDiscovery.SendDiscoveryRequest(m_lookupServer);
        }

        bool IsLookingUpServer(IPEndPoint endPoint)
        {
            return Time.realtimeSinceStartup - m_timeWhenLookedUpServer < this.refreshInterval 
                && m_lookupServer != null 
                && m_lookupServer.Equals(endPoint);
        }

        void OnDiscoveredServer(NetworkDiscovery.DiscoveryInfo info)
        {
            if (!IsRefreshing && !IsLookingUpServer(info.EndPoint))
                return;

            int index = m_discoveredServers.FindIndex(item => item.EndPoint.Equals(info.EndPoint));
            if(index < 0)
            {
                // server is not in the list
                // add it
                m_discoveredServers.Add(info);
            }
            else
            {
                // server is in the list
                // update it
                m_discoveredServers[index] = info;
            }

        }

    }

}
