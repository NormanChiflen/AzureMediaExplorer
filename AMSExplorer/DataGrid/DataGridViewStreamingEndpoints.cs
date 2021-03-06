//----------------------------------------------------------------------------------------------
//    Copyright 2021 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//---------------------------------------------------------------------------------------------


using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AMSExplorer
{
    public class DataGridViewStreamingEndpoints : DataGridView
    {
        private readonly List<StatusInfo> ListStatus = new();
        private static SortableBindingList<StreamingEndpointEntry> _MyObservStreamingEndpoints;
        private static IEnumerable<StreamingEndpoint> streamingendpoints;
        private static bool _initialized = false;
        private static string _filterstreamingendpointsstate = "All";
        private static string _searchinname = string.Empty;
        private static string _timefilter = FilterTime.LastWeek;

        public string FilterStreamingEndpointsState
        {
            get => _filterstreamingendpointsstate;
            set => _filterstreamingendpointsstate = value;

        }
        public string SearchInName
        {
            get => _searchinname;
            set => _searchinname = value;

        }
        public bool Initialized => _initialized;
        public string TimeFilter
        {
            get => _timefilter;
            set => _timefilter = value;
        }
        public int DisplayedCount => _MyObservStreamingEndpoints.Count;
        public List<StreamingEndpoint> GetDisplayedStreamingEndpoints(AMSClientV3 amsClient)
        {
            List<StreamingEndpoint> list = new();
            foreach (StreamingEndpointEntry se in _MyObservStreamingEndpoints)
            {
                try
                {
                    StreamingEndpoint detailedSE = Task.Run(() => amsClient.GetStreamingEndpointAsync(se.Name)).GetAwaiter().GetResult();
                    list.Add(detailedSE);
                }
                catch
                {

                }
            }
            return list;
        }

        public async Task InitAsync(AMSClientV3 amsClient)
        {
            IEnumerable<StreamingEndpointEntry> originquery;

            Microsoft.Rest.Azure.IPage<StreamingEndpoint> ses = await amsClient.AMSclient.StreamingEndpoints.ListAsync(amsClient.credentialsEntry.ResourceGroup, amsClient.credentialsEntry.AccountName);

            originquery = ses.Select(o => new
                          StreamingEndpointEntry
            {
                Name = o.Name,
                Id = o.Id,
                Description = o.Description,
                CDN = ((bool)o.CdnEnabled) ? StreamingEndpointInformation.ReturnDisplayedProvider(o.CdnProvider) ?? "CDN" : string.Empty,
                ScaleUnits = StreamingEndpointInformation.ReturnTypeSE(o) != StreamingEndpointInformation.StreamEndpointType.Premium ? string.Empty : o.ScaleUnits.ToString(),
                Type = StreamingEndpointInformation.ReturnTypeSE(o),
                State = (StreamingEndpointResourceState)o.ResourceState,
                LastModified = ((DateTime)o.LastModified).ToLocalTime()
            });

            SortableBindingList<StreamingEndpointEntry> MyObservOriginInPage = new(originquery.Take(0).ToList());
            DataSource = MyObservOriginInPage;
            Columns["Id"].Visible = false;
            Columns["Name"].Width = 300;
            Columns["State"].Width = 100;
            Columns["CDN"].Width = 120;
            Columns["Description"].Width = 230;
            Columns["ScaleUnits"].Width = 100;
            Columns["ScaleUnits"].HeaderText = "Streaming Units";
            Columns["LastModified"].Width = 150;
            Columns["LastModified"].HeaderText = "Last Modified";

            _initialized = true;
        }


        public async Task RefreshStreamingEndpointAsync(StreamingEndpoint streamingEndpoint, AMSClientV3 amsClient)
        {
            int index = -1;
            foreach (StreamingEndpointEntry CE in _MyObservStreamingEndpoints) // let's search for index
            {
                if (CE.Id == streamingEndpoint.Id)
                {
                    index = _MyObservStreamingEndpoints.IndexOf(CE);
                    break;
                }
            }

            if (index >= 0) // we found it
            { // we update the observation collection

                try
                {
                    streamingEndpoint = await amsClient.GetStreamingEndpointAsync(streamingEndpoint.Name); //refresh
                    _MyObservStreamingEndpoints[index].State = (StreamingEndpointResourceState)streamingEndpoint.ResourceState;
                    _MyObservStreamingEndpoints[index].Description = streamingEndpoint.Description;
                    _MyObservStreamingEndpoints[index].LastModified = ((DateTime)streamingEndpoint.LastModified).ToLocalTime();
                    _MyObservStreamingEndpoints[index].Type = StreamingEndpointInformation.ReturnTypeSE(streamingEndpoint);
                    _MyObservStreamingEndpoints[index].CDN = ((bool)streamingEndpoint.CdnEnabled) ? StreamingEndpointInformation.ReturnDisplayedProvider(streamingEndpoint.CdnProvider) ?? "CDN" : string.Empty;
                    _MyObservStreamingEndpoints[index].ScaleUnits = StreamingEndpointInformation.ReturnTypeSE(streamingEndpoint) != StreamingEndpointInformation.StreamEndpointType.Premium ? string.Empty : streamingEndpoint.ScaleUnits.ToString();
                    BeginInvoke(new Action(() => Refresh()));
                }
               catch
                {

                }
            }
        }


        public async Task RefreshStreamingEndpointsAsync(AMSClientV3 amsClient)
        {
            if (!_initialized)
            {
                return;
            }

            BeginInvoke(new Action(() => FindForm().Cursor = Cursors.WaitCursor));

            IEnumerable<StreamingEndpointEntry> endpointquery;


            streamingendpoints = await amsClient.AMSclient.StreamingEndpoints.ListAsync(amsClient.credentialsEntry.ResourceGroup, amsClient.credentialsEntry.AccountName);

            try
            {
                int c = streamingendpoints.Count();
            }
            catch (Exception e)
            {
                MessageBox.Show("There is a problem when connecting to Azure Media Services. Application will close. " + e.Message);
                Environment.Exit(0);
            }

            endpointquery = from c in streamingendpoints
                            select new StreamingEndpointEntry
                            {
                                Name = c.Name,
                                Id = c.Id,
                                Description = c.Description,
                                CDN = (bool)c.CdnEnabled ? StreamingEndpointInformation.ReturnDisplayedProvider(c.CdnProvider) ?? "CDN" : string.Empty,
                                ScaleUnits = StreamingEndpointInformation.ReturnTypeSE(c) != StreamingEndpointInformation.StreamEndpointType.Premium ? string.Empty : c.ScaleUnits.ToString(),
                                State = (StreamingEndpointResourceState)c.ResourceState,
                                LastModified = ((DateTime)c.LastModified).ToLocalTime(),
                                Type = StreamingEndpointInformation.ReturnTypeSE(c)
                            };

            _MyObservStreamingEndpoints = new SortableBindingList<StreamingEndpointEntry>(endpointquery.ToList());
            BeginInvoke(new Action(() => DataSource = _MyObservStreamingEndpoints));
            BeginInvoke(new Action(() => FindForm().Cursor = Cursors.Default));
        }
    }
}