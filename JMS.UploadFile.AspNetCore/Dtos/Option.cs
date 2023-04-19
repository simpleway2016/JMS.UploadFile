using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace JMS.UploadFile.AspNetCore
{
    public class Option
    {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="routeName">路由名称</param>
		public Option(string routeName) {			
			if (Regex.Match(routeName, @"[\w]+").Value != routeName)
				throw new Exception("路由名称不允许特殊字符");
            this.RouteName = routeName;
        }

		public Type ReceptionType { get; set; }

		public string RouteName { get;  }

		private long _MaxFileLength = 1024*1024*20;
		/// <summary>
		/// 文件大小的上限，默认20m
		/// </summary>
		public long MaxFileLength
		{
			get => _MaxFileLength;
			set
			{
				if (_MaxFileLength != value)
				{
					_MaxFileLength = value;
				}
			}
		}

		/// <summary>
		/// 允许断点续传
		/// </summary>
		public bool AllowResume
		{
			get; set;
		} = true;
    }
}
