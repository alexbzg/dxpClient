﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XmlConfigNS;
using System.Windows.Forms;
using SerializationNS;

namespace dxpClient
{
    class HTTPService
    {
        private static int pingIterval = 60 * 1000;
        HttpClient client = new HttpClient();
        string srvURI;
        System.Threading.Timer pingTimer;
        ConcurrentQueue<QSO> qsoQueue = new ConcurrentQueue<QSO>();
        private string unsentFilePath = Application.StartupPath + "\\unsent.dat";
        private volatile bool _connected;
        private DXpConfig config;
        public bool connected {  get { return _connected; } }
        public EventHandler<EventArgs> connectionStateChanged;


        public HTTPService( string _srvURI, DXpConfig _config)
        {
            srvURI = _srvURI;
            config = _config;
            pingTimer = new System.Threading.Timer( obj => ping(), null, 1, Timeout.Infinite);
            List<QSO> unsentQSOs = ProtoBufSerialization.Read<List<QSO>>(unsentFilePath);
            if (unsentQSOs != null && unsentQSOs.Count > 0)
                Task.Run( () =>
                {
                    foreach (QSO qso in unsentQSOs)
                        postQso(qso);
                    saveUnsent();
                });
        }

        private async Task<bool> post(string sContent)
        {
            System.Diagnostics.Debug.WriteLine(sContent);
            pingTimer.Change(Timeout.Infinite, Timeout.Infinite);
            HttpContent content = new StringContent(sContent);
            bool result = false;
            try
            {
                HttpResponseMessage response = await client.PostAsync(srvURI, content);
                result = response.IsSuccessStatusCode;
                System.Diagnostics.Debug.WriteLine(response.ToString());
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            if (_connected != result )
            {
                _connected = result;
                if (_connected)
                    await processQueue();
                connectionStateChanged?.Invoke(this, new EventArgs());
            }
            pingTimer.Change(pingIterval, Timeout.Infinite);
            return result;
        }

        public async Task postQso( QSO qso)
        {
            if (qsoQueue.IsEmpty)
            {
                bool response = await post(qso.toJSON());
                if (!response)
                    addToQueue(qso);
            }
            else
                addToQueue(qso);
        }

        private void addToQueue( QSO qso)
        {
            qsoQueue.Enqueue(qso);
            saveUnsent();
        }

        private void saveUnsent()
        {
            ProtoBufSerialization.Write<List<QSO>>(unsentFilePath, qsoQueue.ToList());
        }

        private async Task processQueue()
        {
            while (!qsoQueue.IsEmpty)
            {
                qsoQueue.TryPeek(out QSO qso);
                bool r = await post(qso.toJSON());
                if (r)
                {
                    qsoQueue.TryDequeue(out qso);
                    saveUnsent();
                }
                else
                    break;
            }
        }

        public async Task ping()
        {
            System.Diagnostics.Debug.WriteLine("Ping!");
            await post( "{\"status\": " + config.toJSON() + "}");
        }
    }

}
