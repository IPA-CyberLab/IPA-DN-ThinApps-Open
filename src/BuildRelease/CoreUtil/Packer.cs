﻿// Thin Telework System Source Code
// 
// License: The Apache License, Version 2.0
// https://www.apache.org/licenses/LICENSE-2.0
// 
// Copyright (c) IPA CyberLab of Industrial Cyber Security Center.
// Copyright (c) Daiyuu Nobori.
// Copyright (c) SoftEther VPN Project, University of Tsukuba, Japan.
// Copyright (c) SoftEther Corporation.
// Copyright (c) all contributors on IPA-DN-Ultra Library and SoftEther VPN Project in GitHub.
// 
// All Rights Reserved.
// 
// DISCLAIMER
// ==========
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
// THIS SOFTWARE IS DEVELOPED IN JAPAN, AND DISTRIBUTED FROM JAPAN, UNDER
// JAPANESE LAWS. YOU MUST AGREE IN ADVANCE TO USE, COPY, MODIFY, MERGE, PUBLISH,
// DISTRIBUTE, SUBLICENSE, AND/OR SELL COPIES OF THIS SOFTWARE, THAT ANY
// JURIDICAL DISPUTES WHICH ARE CONCERNED TO THIS SOFTWARE OR ITS CONTENTS,
// AGAINST US (IPA CYBERLAB, SOFTETHER PROJECT, SOFTETHER CORPORATION, DAIYUU NOBORI
// OR OTHER SUPPLIERS), OR ANY JURIDICAL DISPUTES AGAINST US WHICH ARE CAUSED BY ANY
// KIND OF USING, COPYING, MODIFYING, MERGING, PUBLISHING, DISTRIBUTING, SUBLICENSING,
// AND/OR SELLING COPIES OF THIS SOFTWARE SHALL BE REGARDED AS BE CONSTRUED AND
// CONTROLLED BY JAPANESE LAWS, AND YOU MUST FURTHER CONSENT TO EXCLUSIVE
// JURISDICTION AND VENUE IN THE COURTS SITTING IN TOKYO, JAPAN. YOU MUST WAIVE
// ALL DEFENSES OF LACK OF PERSONAL JURISDICTION AND FORUM NON CONVENIENS.
// PROCESS MAY BE SERVED ON EITHER PARTY IN THE MANNER AUTHORIZED BY APPLICABLE
// LAW OR COURT RULE.
// 
// USE ONLY IN JAPAN. DO NOT USE THIS SOFTWARE IN ANOTHER COUNTRY UNLESS YOU HAVE
// A CONFIRMATION THAT THIS SOFTWARE DOES NOT VIOLATE ANY CRIMINAL LAWS OR CIVIL
// RIGHTS IN THAT PARTICULAR COUNTRY. USING THIS SOFTWARE IN OTHER COUNTRIES IS
// COMPLETELY AT YOUR OWN RISK. THE IPA CYBERLAB HAS DEVELOPED AND
// DISTRIBUTED THIS SOFTWARE TO COMPLY ONLY WITH THE JAPANESE LAWS AND EXISTING
// CIVIL RIGHTS INCLUDING PATENTS WHICH ARE SUBJECTS APPLY IN JAPAN. OTHER
// COUNTRIES' LAWS OR CIVIL RIGHTS ARE NONE OF OUR CONCERNS NOR RESPONSIBILITIES.
// WE HAVE NEVER INVESTIGATED ANY CRIMINAL REGULATIONS, CIVIL LAWS OR
// INTELLECTUAL PROPERTY RIGHTS INCLUDING PATENTS IN ANY OF OTHER 200+ COUNTRIES
// AND TERRITORIES. BY NATURE, THERE ARE 200+ REGIONS IN THE WORLD, WITH
// DIFFERENT LAWS. IT IS IMPOSSIBLE TO VERIFY EVERY COUNTRIES' LAWS, REGULATIONS
// AND CIVIL RIGHTS TO MAKE THE SOFTWARE COMPLY WITH ALL COUNTRIES' LAWS BY THE
// PROJECT. EVEN IF YOU WILL BE SUED BY A PRIVATE ENTITY OR BE DAMAGED BY A
// PUBLIC SERVANT IN YOUR COUNTRY, THE DEVELOPERS OF THIS SOFTWARE WILL NEVER BE
// LIABLE TO RECOVER OR COMPENSATE SUCH DAMAGES, CRIMINAL OR CIVIL
// RESPONSIBILITIES. NOTE THAT THIS LINE IS NOT LICENSE RESTRICTION BUT JUST A
// STATEMENT FOR WARNING AND DISCLAIMER.
// 
// READ AND UNDERSTAND THE 'WARNING.TXT' FILE BEFORE USING THIS SOFTWARE.
// SOME SOFTWARE PROGRAMS FROM THIRD PARTIES ARE INCLUDED ON THIS SOFTWARE WITH
// LICENSE CONDITIONS WHICH ARE DESCRIBED ON THE 'THIRD_PARTY.TXT' FILE.
// 
// ---------------------
// 
// If you find a bug or a security vulnerability please kindly inform us
// about the problem immediately so that we can fix the security problem
// to protect a lot of users around the world as soon as possible.
// 
// Our e-mail address for security reports is:
// daiyuu.securityreport [at] dnobori.jp
// 
// Thank you for your cooperation.


using System;
using System.Threading;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Text;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace CoreUtil
{
	public enum PackerFileFormat
	{
		ZipRaw,
		ZipCompressed,
		Tar,
		TarGZip,
	}

	public delegate bool ProgressDelegate(string fileNameFullPath, string fileNameRelative, int currentFileNum, int totalFileNum);

	public static class Packer
	{
		public static byte[] PackDir(PackerFileFormat format, string rootDirPath, string appendPrefixDirName)
		{
			return PackDir(format, rootDirPath, appendPrefixDirName, null);
		}
		public static byte[] PackDir(PackerFileFormat format, string topDirPath, string appendPrefixDirName, ProgressDelegate proc)
		{
			string[] fileList = Directory.GetFiles(topDirPath, "*", SearchOption.AllDirectories);
			List<string> relativeFileList = new List<string>();

			foreach (string fileName in fileList)
			{
				string relativePath = IO.GetRelativeFileName(fileName, topDirPath);

				if (Str.IsEmptyStr(appendPrefixDirName) == false)
				{
					relativePath = IO.RemoteLastEnMark(appendPrefixDirName) + "\\" + relativePath;
				}

				relativeFileList.Add(relativePath);
			}

			return PackFiles(format, fileList, relativeFileList.ToArray(), proc);
		}

		public static byte[] PackFiles(PackerFileFormat format, string[] srcFileNameList, string[] relativeNameList)
		{
			return PackFiles(format, srcFileNameList, relativeNameList, null);
		}
		public static byte[] PackFiles(PackerFileFormat format, string[] srcFileNameList, string[] relativeNameList, ProgressDelegate proc)
		{
			if (srcFileNameList.Length != relativeNameList.Length)
			{
				throw new ApplicationException("srcFileNameList.Length != relativeNameList.Length");
			}

			int num = srcFileNameList.Length;
			int i;

			ZipPacker zip = new ZipPacker();
			TarPacker tar = new TarPacker();

			for (i = 0; i < num; i++)
			{
				if (proc != null)
				{
					bool ret = proc(srcFileNameList[i], relativeNameList[i], i, num);

					if (ret == false)
					{
						continue;
					}
				}

				byte[] srcData = File.ReadAllBytes(srcFileNameList[i]);
				DateTime date = File.GetLastWriteTime(srcFileNameList[i]);

				switch (format)
				{
					case PackerFileFormat.Tar:
					case PackerFileFormat.TarGZip:
						tar.AddFileSimple(relativeNameList[i], srcData, 0, srcData.Length, date);
						break;

					case PackerFileFormat.ZipRaw:
					case PackerFileFormat.ZipCompressed:
						zip.AddFileSimple(relativeNameList[i], date, FileAttributes.Normal, srcData, (format == PackerFileFormat.ZipCompressed));
						break;
				}
			}

			switch (format)
			{
				case PackerFileFormat.Tar:
					tar.Finish();
					return tar.GeneratedData.Read();

				case PackerFileFormat.TarGZip:
					tar.Finish();
					return tar.CompressToGZip();

				case PackerFileFormat.ZipCompressed:
				case PackerFileFormat.ZipRaw:
					zip.Finish();
					return zip.GeneratedData.Read();

				default:
					throw new ApplicationException("format");
			}
		}
	}
}
