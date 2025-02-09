/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

using System.Collections.Generic;
using System.Xml;

namespace QtVsTools.Core
{
    public class QrcParser
    {
        private readonly string qrcFileName;
        private readonly Stack<QrcPrefix> prefixes;

        public List<QrcPrefix> Prefixes { get; }

        public QrcParser(string fileName)
        {
            qrcFileName = fileName;
            prefixes = new Stack<QrcPrefix>();
            Prefixes = new List<QrcPrefix>();
        }

        public bool parse()
        {
            var fi = new System.IO.FileInfo(qrcFileName);
            if (!fi.Exists)
                return false;
            try {
                var reader = new XmlTextReader(qrcFileName);
                QrcItem currentItem = null;
                QrcPrefix currentPrefix = null;
                while (reader.Read()) {
                    switch (reader.NodeType) {
                    case XmlNodeType.Element:
                        if (reader.LocalName.ToLower() == "qresource") {
                            currentPrefix = new QrcPrefix();
                            currentPrefix.Prefix = reader.GetAttribute("prefix");
                            currentPrefix.Language = reader.GetAttribute("lang");
                            prefixes.Push(currentPrefix);
                        } else if (reader.LocalName.ToLower() == "file") {
                            currentItem = new QrcItem();
                            currentItem.Alias = reader.GetAttribute("name");
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (reader.LocalName.ToLower() == "qresource") {
                            Prefixes.Add(prefixes.Pop());
                        } else if (reader.LocalName.ToLower() == "file"
                              && prefixes.Peek() != null && currentItem != null) {
                            prefixes.Peek().AddQrcItem(currentItem);
                            currentItem = null;
                        }
                        break;
                    case XmlNodeType.Text:
                        if (currentItem != null)
                            currentItem.Path = reader.Value;
                        break;
                    }
                }
                reader.Close();
            } catch (System.Exception) {
                return false;
            }
            return true;
        }
    }
}
