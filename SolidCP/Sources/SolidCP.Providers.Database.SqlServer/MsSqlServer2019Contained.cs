// Copyright (c) 2016, SolidCP
// SolidCP is distributed under the Creative Commons Share-alike license
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// - Redistributions of source code must  retain  the  above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form  must  reproduce the  above  copyright  notice,
//   this list of conditions  and  the  following  disclaimer in  the documentation
//   and/or other materials provided with the distribution.
//
// - Neither  the  name  of  SolidCP  nor   the   names  of  its
//   contributors may be used to endorse or  promote  products  derived  from  this
//   software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING,  BUT  NOT  LIMITED TO, THE IMPLIED
// WARRANTIES  OF  MERCHANTABILITY   AND  FITNESS  FOR  A  PARTICULAR  PURPOSE  ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL,  SPECIAL,  EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO,  PROCUREMENT  OF  SUBSTITUTE  GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)  HOWEVER  CAUSED AND ON
// ANY  THEORY  OF  LIABILITY,  WHETHER  IN  CONTRACT,  STRICT  LIABILITY,  OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE)  ARISING  IN  ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

﻿using System;
using System.Collections.Generic;
using System.Data;
 using System.IO;
 using SolidCP.Providers.Utils;

namespace SolidCP.Providers.Database
{
    public class MsSqlServer2019Contained : MsSqlServer2019
    {
        public override bool IsInstalled()
        {
            return true;
        }

        public override void CreateDatabase(SqlDatabase database)
        {
            if (database.Users == null)
                database.Users = new string[0];

            string commandText = "";
            if (String.IsNullOrEmpty(database.Location))
            {
                // load default location
                SqlDatabase dbMaster = GetDatabase("master");
                database.Location = Path.GetDirectoryName(dbMaster.DataPath);
            }
            else
            {
                // subst vars
                database.Location = FileUtils.EvaluateSystemVariables(database.Location);

                // verify folder exists
                if (!Directory.Exists(database.Location))
                    Directory.CreateDirectory(database.Location);
            }

            string collation = String.IsNullOrEmpty(DatabaseCollation) ? "" : " COLLATE " + DatabaseCollation;

            // create command
            string dataFile = Path.Combine(database.Location, database.Name) + "_data.mdf";
            string logFile = Path.Combine(database.Location, database.Name) + "_log.ldf";


            commandText = string.Format("CREATE DATABASE [{0}]" +
                                        "CONTAINMENT = PARTIAL" +
                                        " ON PRIMARY ( NAME = '{1}_data', {2})" +
                                        " LOG ON ( NAME = '{3}_log', {4}){5};",
                database.Name,
                base.EscapeSql(database.Name),
                base.CreateFileNameString(dataFile, database.DataSize),
                base.EscapeSql(database.Name),
                base.CreateFileNameString(logFile, database.LogSize),
                collation);


            // create database
            ExecuteNonQuery(commandText);
        }

        public override void TruncateDatabase(string databaseName)
        {
            SqlDatabase database = GetDatabase(databaseName);
            ExecuteNonQuery(String.Format(@"USE [{0}];DBCC SHRINKFILE ('{1}', 1);",
                databaseName,  database.LogName));
        }

        public override string[] GetUsers()
        {
            DataTable dt = ExecuteQuery("select name from sys.sql_logins where name not like '##MS%' and IS_SRVROLEMEMBER ('sysadmin',name) = 0").Tables[0];
            List<string> users = new List<string>();
            foreach (DataRow dr in dt.Rows)
                users.Add(dr["name"].ToString());
            return users.ToArray();
        }
    }
}
