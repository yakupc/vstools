﻿/****************************************************************************
**
** Copyright (C) 2018 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Core
{
    using VisualStudio;

    public class WaitDialog : IDisposable
    {
        static IVsThreadedWaitDialogFactory factory = null;

        private IVsThreadedWaitDialog2 VsWaitDialog { get; set; }

        private bool Running { get; set; }

        bool? vsDialogCanceled = null;

        public bool Canceled
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (vsDialogCanceled.HasValue)
                    return vsDialogCanceled.Value;

                if (VsWaitDialog == null)
                    return false;

                int res = VsWaitDialog.HasCanceled(out bool canceled);
                if (res != VSConstants.S_OK)
                    return false;

                return canceled;
            }
            private set => vsDialogCanceled = value;
        }

        private WaitDialog() { }

        static WaitDialog Create(IVsThreadedWaitDialogFactory dialogFactory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (factory == null) {
                factory = dialogFactory ?? VsServiceProvider
                    .GetService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>();
                if (factory == null)
                    return null;
            }

            factory.CreateInstance(out IVsThreadedWaitDialog2 vsWaitDialog);
            if (vsWaitDialog == null)
                return null;

            return new WaitDialog
            {
                VsWaitDialog = vsWaitDialog,
                Running = true,
            };
        }

        public static WaitDialog Start(
            string caption,
            string message,
            string progressText = null,
            string statusBarText = null,
            int delay = 0,
            bool isCancelable = false,
            bool showMarqueeProgress = true,
            IVsThreadedWaitDialogFactory dialogFactory = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dialog = Create(dialogFactory);
            if (dialog == null)
                return null;

            var res = dialog.VsWaitDialog.StartWaitDialog(caption, message, progressText,
                    null, statusBarText, delay, isCancelable, showMarqueeProgress);

            if (res != VSConstants.S_OK)
                return null;

            return dialog;
        }

        public static WaitDialog StartWithProgress(
            string caption,
            string message,
            int totalSteps,
            int currentStep = 0,
            string progressText = null,
            string statusBarText = null,
            int delay = 0,
            bool isCancelable = false,
            IVsThreadedWaitDialogFactory dialogFactory = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dialog = Create(dialogFactory);
            if (dialog == null)
                return null;

            var res = dialog.VsWaitDialog.StartWaitDialogWithPercentageProgress(
                caption, message, progressText, null, statusBarText,
                isCancelable, delay, totalSteps, currentStep);

            if (res != VSConstants.S_OK)
                return null;

            return dialog;
        }

        public void Update(
            string message,
            int totalSteps,
            int currentStep,
            string progressText = null,
            string statusBarText = null,
            bool disableCancel = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!Running)
                return;

            int res = VsWaitDialog.UpdateProgress(message, progressText,
                statusBarText, currentStep, totalSteps, disableCancel, out bool canceled);

            if (res != VSConstants.S_OK)
                return;

            if (canceled)
                Stop();
        }

        public void Stop()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!Running)
                return;

            Running = false;
            VsWaitDialog.EndWaitDialog(out int canceled);
            Canceled = (canceled != 0);
        }

        void IDisposable.Dispose()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () => {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Stop();
            });
        }
    }
}
