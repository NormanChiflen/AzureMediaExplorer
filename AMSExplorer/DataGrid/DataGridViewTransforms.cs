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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AMSExplorer
{
    public class DataGridViewTransforms : DataGridView
    {
        private bool _initialized = false;

        private readonly List<string> idsList = new();
        private SynchronizationContext _context;
        private static BindingList<TransformEntry> _MyObservTransformsV3;

        public async Task InitAsync(AMSClientV3 amsClient, SynchronizationContext context)
        {
            _context = context;

            Microsoft.Rest.Azure.IPage<Transform> transformsList = await amsClient.AMSclient.Transforms.ListAsync(amsClient.credentialsEntry.ResourceGroup, amsClient.credentialsEntry.AccountName);

            IEnumerable<Task<TransformEntry>> transforms = transformsList.Select(async a => new TransformEntry(_context)
            {
                Name = a.Name,
                Description = a.Description,
                Jobs = (await amsClient.AMSclient.Jobs.ListAsync(amsClient.credentialsEntry.ResourceGroup, amsClient.credentialsEntry.AccountName, a.Name)).Count(),
                LastModified = a.LastModified.ToLocalTime().ToString("G")
            }
            );

            TransformEntry[] mappedItems = await Task.WhenAll(transforms);

            BindingList<TransformEntry> MyObservTransformthisPageV3 = new(mappedItems);
            DataSource = MyObservTransformthisPageV3;

            Task myTask = Task.Factory.StartNew(() =>
            {
                BeginInvoke(new Action(() =>
                {
                    Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    Columns["Name"].Width = 200;
                    Columns["Description"].Width = 200;
                    Columns["LastModified"].Width = 130;
                }));
            });

            _initialized = true;
        }


        public async Task RefreshTransformsAsync(AMSClientV3 amsClient) // all transforms are refreshed
        {
            if (!_initialized)
            {
                return;
            }

            Debug.WriteLine("Refresh Transforms Start");

            BeginInvoke(new Action(() => FindForm().Cursor = Cursors.WaitCursor));



            IEnumerable<Task<TransformEntry>> transforms = (await amsClient.AMSclient.Transforms.ListAsync(amsClient.credentialsEntry.ResourceGroup, amsClient.credentialsEntry.AccountName)).Select(async a => new TransformEntry(_context)
            {
                Name = a.Name,
                Description = a.Description,
                Outputs = a.Outputs.Count,
                Jobs = (await amsClient.AMSclient.Jobs.ListAsync(amsClient.credentialsEntry.ResourceGroup, amsClient.credentialsEntry.AccountName, a.Name)).Count(),
                LastModified = a.LastModified.ToLocalTime().ToString("G")
            }
          );
            TransformEntry[] mappedItems = await Task.WhenAll(transforms);

            _MyObservTransformsV3 = new BindingList<TransformEntry>(mappedItems);

            BeginInvoke(new Action(() => DataSource = _MyObservTransformsV3));

            Debug.WriteLine("RefreshTransforms End");

            BeginInvoke(new Action(() => FindForm().Cursor = Cursors.Default));
        }

        public async Task<List<Transform>> ReturnSelectedTransformsAsync(AMSClientV3 amsClient)
        {


            List<Transform> SelectedTransforms = new();
            foreach (DataGridViewRow Row in SelectedRows)
            {
                // sometimes, the transform can be null (if just deleted)
                try
                {
                    Transform transform = await amsClient.GetTransformAsync(Row.Cells[Columns["Name"].Index].Value.ToString());
                    SelectedTransforms.Add(transform);
                }
                catch
                {

                }
            }
            SelectedTransforms.Reverse();
            return SelectedTransforms;
        }

        public void SelectTransform(Transform transform)
        {
            ClearSelection();
            foreach (DataGridViewRow Row in Rows)
            {
                if ((string)Row.Cells[0].Value == transform.Name)
                {

                    Row.Selected = true;
                    FirstDisplayedScrollingRowIndex = SelectedRows[0].Index;
                    RefreshGridView();
                    break;
                }
            }
        }

        private void RefreshGridView()
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate ()
                {
                    RefreshGridView();
                });
            }
            else
                Refresh();
        }
    }
}