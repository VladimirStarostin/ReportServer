﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Monik.Common;
using Npgsql;
using ReportService.Entities;
using ReportService.Entities.Dto;
using ReportService.Interfaces.Core;

namespace ReportService.Core
{
    public class PostgreSqlRepository : IRepository
    {
        private readonly IMonik monik;
        private readonly string connectionString;

        public PostgreSqlRepository(string connStr, IMonik monik)
        {
            connectionString = connStr;

            this.monik = monik;
        }

        public async Task<object> GetBaseQueryResult(string query, CancellationToken token)
        {

            await using var connection = new NpgsqlConnection(connectionString);

            dynamic result =
                await connection.QueryFirstAsync<dynamic>(new CommandDefinition(query,
                    commandTimeout: 30, cancellationToken: token));

            var value = (result as IDictionary<string, object>)?.First().Value;
            return value;
        }

        public async Task<List<DtoTaskInstance>> GetAllTaskInstances(long taskId)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            try
            {
                return (await connection.QueryAsync<DtoTaskInstance>(
                        $@"select * from ""TaskInstance"" where ""TaskId""={taskId}"))
                    .ToList();
            }

            catch (Exception e)
            {
                SendAppWarning("Error occured while getting task instances: " +
                               $"{e.Message}");
                throw;
            }
        }

        public DependencyState GetDependencyStateByTaskId(long taskId)
        {
            using var connection = new NpgsqlConnection(connectionString);

            try
            {
                return connection.QueryFirst<DependencyState>(
                    $@"SELECT max(case when ""State""=2 then ""StartTime""+ ""Duration""*INTERVAL'1 ms' 
                                        else null end) LastSuccessfulFinish,
                        count(case when ""State=1"" then 1 else null end) InProcessCount
                        FROM ""TaskInstance"" 
                        where ""TaskId""={taskId}",
                    commandTimeout: 30);
            }

            catch (Exception e)
            {
                SendAppWarning("Error occured while getting task last time: " +
                               $"{e.Message}");
                throw;
            }
        }

        public List<DtoOperInstance> GetTaskOperInstances(long taskInstanceId)
        {
            using var connection = new NpgsqlConnection(connectionString);

            try
            {
                return connection.Query<DtoOperInstance>
                    ($@"select Id,""TaskInstanceId"",""OperationId"",
                            ""StartTime"",""Duration"",""State"",null as DataSet,
                            null as ErrorMessage
                        from ""OperInstance"" where ""TaskInstanceId""={taskInstanceId}",
                        commandTimeout: 60)
                    .ToList();
            }

            catch (Exception e)
            {
                SendAppWarning("Error occured while getting operation instances: " +
                               $"{e.Message}");
                throw;
            }
        }

        public DtoOperInstance GetFullOperInstanceById(long operInstanceId)
        {
            using var connection = new NpgsqlConnection(connectionString);

            try
            {
                return connection.QueryFirst<DtoOperInstance>
                ($@"select oi.Id,""TaskInstanceId"",""OperationId"",
                    ""StartTime"",""Duration"",""State"",""DataSet"",
                    ""ErrorMessage"",""Name"" as OperName
                from ""OperInstance"" oi
                join ""Operation"" op
                    on oi.""OperationId"" = op.Id
                where oi.Id ={operInstanceId}",
                    commandTimeout: 60);
            }

            catch (Exception e)
            {
                SendAppWarning("Error occured while getting operation instance data: " +
                               $"{e.Message}");
                throw;
            }
        }

        public List<T> GetListEntitiesByDtoType<T>() where T : class, IDtoEntity
        {
            using var connection = new NpgsqlConnection(connectionString);

            var tableName = typeof(T).Name.Remove(0, 3);

            try
            {
                return connection.GetAll<T>().ToList();
            }

            catch (Exception e)
            {
                SendAppWarning("Error occured while getting " +
                               $"{tableName} list: {e.Message}");
                return null;
            }
        }

        public long CreateEntity<T>(T entity) where T : class, IDtoEntity
        {
            using var connection = new NpgsqlConnection(connectionString);

            var tableName = typeof(T).Name.Remove(0, 3);

            try
            {
                return connection.Insert(entity,
                    commandTimeout: 60);
            }

            catch (Exception e)
            {
                SendAppWarning("Error occured while creating new " +
                               $"{tableName} record: {e.Message}");
                return default;
            }
        }

        public long CreateTask(DtoTask task, params DtoOperation[] bindedOpers)
        {
            using var connection = new NpgsqlConnection(connectionString);

            long newTaskId;

            connection.Open();

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    newTaskId = connection.Insert(task,
                        commandTimeout: 60, transaction: transaction);
                    DefaultTypeMap.MatchNamesWithUnderscores = true;

                    if (bindedOpers != null)

                        foreach (var oper in bindedOpers)
                        {
                            oper.TaskId = newTaskId;
                        }

                    connection.Insert(bindedOpers,
                        commandTimeout: 60, transaction: transaction);

                    transaction.Commit();
                }

                catch (Exception e)
                {
                    transaction.Rollback();

                    SendAppWarning("Error occured while creating new Task" +
                                   $" record: {e.Message}");
                    throw;
                }
            }

            return newTaskId;
        }

        public void UpdateEntity<T>(T entity) where T : class, IDtoEntity
        {
            using var connection = new NpgsqlConnection(connectionString);

            var tableName = typeof(T).Name.Remove(0, 3);

            try
            {
                connection.Update(entity, commandTimeout: 60);
            }

            catch (Exception e)
            {
                SendAppWarning("Error occured while updating " +
                               $"{tableName} record: {e.Message}");
            }
        }

        public void UpdateTask(DtoTask task, params DtoOperation[] bindedOpers)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var currentOperIds = connection.Query<long>
            ($@"select Id from ""Operation"" where ""TaskId""={task.Id}
                and ""IsDeleted""=false",
                commandTimeout: 60);

            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                var newOperIds = bindedOpers.Select(oper => oper.Id);

                var operIdsToDelete = currentOperIds.Except(newOperIds);

                var opersToUpdate = bindedOpers.Where(oper =>
                    newOperIds.Intersect(currentOperIds).Contains(oper.Id));

                var opersToWrite = bindedOpers.Where(oper =>
                    newOperIds.Except(currentOperIds).Contains(oper.Id));

                connection.Update(task, commandTimeout: 60, transaction: transaction);

                if (operIdsToDelete.Any())
                    connection.Execute(
                        $@"Update ""Operation"" set ""IsDeleted""=True where ""TaskId""={task.Id} and
                            Id in ({string.Join(",", operIdsToDelete)})",
                        commandTimeout: 60, transaction: transaction);

                connection.Update(opersToUpdate, commandTimeout: 60, transaction: transaction);

                connection.Insert(opersToWrite, commandTimeout: 60, transaction: transaction);

                transaction.Commit();
            }

            catch (Exception e)
            {
                transaction.Rollback();

                SendAppWarning("Error occured while updating Task" +
                               $" record: {e.Message}");
                throw;
            }
        }

        public void DeleteEntity<T, TKey>(TKey id) where T : IDtoEntity
        {
            using var connection = new NpgsqlConnection(connectionString);

            var type = typeof(T);
            var tableName = type.Name.Remove(0, 3);

            connection.Open();

            switch (true)
            {
                case { } when type == typeof(DtoTaskInstance):
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            connection.Execute($@"delete from ""OperInstance"" where ""TaskInstanceId""={id}",
                                commandTimeout: 60, transaction: transaction);

                            connection.Execute($@"delete from ""TaskInstance"" where Id={id}",
                                commandTimeout: 60, transaction: transaction);

                            transaction.Commit();
                        }

                        catch (Exception e)
                        {
                            SendAppWarning("Error occured while deleting Task instance" +
                                           $" record: {e.Message}");
                            transaction.Rollback();
                            throw;
                        }
                    }

                    break;

                case { } when type == typeof(DtoTask):
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {

                            connection.Execute($@"delete from ""OperInstance"" where ""TaskInstanceId"" in
                                            (select Id from ""TaskInstance"" where ""TaskId""={id})",
                                commandTimeout: 60, transaction: transaction);

                            connection.Execute($@"delete from ""TaskInstance"" where ""TaskID""={id}",
                                commandTimeout: 60, transaction: transaction);

                            connection.Execute($@"delete from ""Operation"" where ""TaskID""={id}",
                                commandTimeout: 60, transaction: transaction);

                            connection.Execute($@"delete from ""Task"" where Id={id}",
                                commandTimeout: 60, transaction: transaction);

                            transaction.Commit();
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();

                            SendAppWarning("Error occured while deleting Task" +
                                           $" record: {e.Message}");
                            throw;
                        }
                    }

                    break;

                default:
                    try
                    {
                        connection.Execute($@"delete from ""{tableName}"" where Id={id}",
                            commandTimeout: 60);
                    }

                    catch (Exception e)
                    {
                        SendAppWarning($"Error occured while deleting {tableName}" +
                                       $" record: {e.Message}");
                        throw;
                    }

                    break;
            }

            //case bool _ when type == typeof(DtoOperation): //todo:do we really need this method?
            //break;

            //case bool _ when type == typeof(DtoTelegramChannel): //todo:method
            //break;

            //case bool _ when type == typeof(DtoRecepientGroup): //todo:method
            //try
            //{
            //    SimpleCommand.ExecuteNonQuery(connStr,
            //        $@"delete RecepientGroup where Id={id}");
            //}
        }

        public List<long> UpdateOperInstancesAndGetIds()
        {
            using var connection = new NpgsqlConnection(connectionString);

            var ids = connection.Query<long>(@"UPDATE ""OperInstance""
            SET ""State""=3,""ErrorMessage""='Unknown error.The service was probably stopped during the task execution.'            
            WHERE ""State""=1
			RETURNING Id").ToList();

            return ids;
        }

        public List<long> UpdateTaskInstancesAndGetIds()
        {
            using var connection = new NpgsqlConnection(connectionString);

            var ids = connection.Query<long>(@"UPDATE ""TaskInstance""
            SET ""State""=3
            WHERE ""State""=1
            RETURNING Id").ToList();

            return ids;
        }

        private void SendAppWarning(string msg)
        {
            monik.ApplicationWarning(msg);
            Console.WriteLine(msg);
        }

        public void CreateBase(string baseConnStr)
        {
            using var connection = new NpgsqlConnection(baseConnStr);

            // TODO: check db exists ~find way to cut redundant code 
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS ""OperTemplate""
                (Id INT GENERATED ALWAYS AS IDENTITY (START WITH 1 INCREMENT BY 1),
                ""ImplementationType"" VARCHAR(255) NOT NULL,
                ""Name"" VARCHAR(255) NOT NULL,
                ""ConfigTemplate"" TEXT NOT NULL,
                CONSTRAINT ""PK_OperTemplate_Id"" PRIMARY KEY(Id));
                CLUSTER ""OperTemplate"" USING ""PK_OperTemplate_Id"";");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS ""RecepientGroup""
                (Id INT GENERATED ALWAYS AS IDENTITY (START WITH 1 INCREMENT BY 1),
	            ""Name"" VARCHAR(127) NOT NULL,
	            ""Addresses"" VARCHAR(4000) NOT NULL,
	            ""AddressesBcc"" VARCHAR(4000) NULL,
                CONSTRAINT ""PK_RecepientGroup_Id"" PRIMARY KEY(Id));
				CLUSTER ""RecepientGroup"" USING ""PK_RecepientGroup_Id"";");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS ""TelegramChannel""
                (Id BIGINT GENERATED ALWAYS AS IDENTITY (START WITH 1 INCREMENT BY 1),
                ""Name"" VARCHAR(127) NOT NULL,
                ""Description"" VARCHAR(255) NULL,
                ""ChatId"" BIGINT NOT NULL,
                ""Type"" SMALLINT NOT NULL,
                CONSTRAINT ""PK_TelegramChannel_Id"" PRIMARY KEY(Id));
                CLUSTER ""TelegramChannel"" USING ""PK_TelegramChannel_Id"";");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS ""Schedule""
                (Id INT GENERATED ALWAYS AS IDENTITY (START WITH 1 INCREMENT BY 1),
                ""Name"" VARCHAR(127) NOT NULL,
                ""Schedule"" VARCHAR(255) NOT NULL,
                CONSTRAINT ""PK_Schedule_Id"" PRIMARY KEY(Id));
				CLUSTER ""Schedule"" USING ""PK_Schedule_Id"";");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS ""Task""
                (Id BIGINT GENERATED ALWAYS AS IDENTITY (START WITH 1 INCREMENT BY 1),
                ""Name"" VARCHAR(127) NOT NULL,
                ""ScheduleId"" INT NULL,
                ""Parameters"" VARCHAR(1023) NULL,
                ""DependsOn"" VARCHAR(1023) NULL,
                CONSTRAINT ""PK_Task_Id"" PRIMARY KEY(Id),
                CONSTRAINT ""FK_Task_Schedule"" FOREIGN KEY(""ScheduleId"") 
                REFERENCES ""Schedule""(Id)
                );
				CLUSTER ""Task"" USING ""PK_Task_Id"";");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS ""Operation""
                (Id BIGINT GENERATED ALWAYS AS IDENTITY (START WITH 1 INCREMENT BY 1),
                ""TaskId"" BIGINT NOT NULL,
                ""Number"" SMALLINT NOT NULL,
                ""Name"" VARCHAR(255) NOT NULL,
                ""ImplementationType"" VARCHAR(255) NOT NULL,
                ""IsDefault"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""Config"" TEXT NOT NULL,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE,
                CONSTRAINT ""PK_Operation_Id"" PRIMARY KEY(Id),
                CONSTRAINT ""FK_Operation_Task"" FOREIGN KEY(""TaskId"") 
                REFERENCES ""Task""(Id));
				CLUSTER ""Operation"" USING ""PK_Operation_Id"";
                CREATE INDEX IF NOT EXISTS ""idx_Operation_TaskId"" ON ""Operation""(""TaskId"" ASC);");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS ""TaskInstance""
                (Id BIGINT GENERATED ALWAYS AS IDENTITY (START WITH 1 INCREMENT BY 1),
                ""TaskId"" BIGINT NOT NULL,
                ""StartTime"" TIMESTAMP(3) NOT NULL,
                ""Duration"" INT NOT NULL,
                ""State"" INT NOT NULL,
                CONSTRAINT ""PK_TaskInstance_Id"" PRIMARY KEY(Id) ,
                CONSTRAINT ""FK_TaskInstance_Task"" FOREIGN KEY(""TaskId"")
                REFERENCES ""Task""(Id));
				CLUSTER ""TaskInstance"" USING ""PK_TaskInstance_Id"";
                CREATE INDEX IF NOT EXISTS ""idx_TaskInstance_TaskId"" ON ""TaskInstance""(""TaskId"" ASC);");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS ""OperInstance""
				(Id BIGINT GENERATED ALWAYS AS IDENTITY (START WITH 1 INCREMENT BY 1),
	            ""TaskInstanceId"" BIGINT NOT NULL,
                ""OperationId"" BIGINT NOT NULL,
                ""StartTime"" TIMESTAMP(3) NOT NULL,
                ""Duration"" INT NOT NULL,
                ""State"" INT NOT NULL,
	            ""DataSet"" BYTEA NULL,
                ""ErrorMessage"" VARCHAR(1023) NULL,
                CONSTRAINT ""PK_OperInstance_Id"" PRIMARY KEY(Id),
                CONSTRAINT ""FK_OperInstance_Operation"" FOREIGN KEY(""OperationId"") 
                REFERENCES ""Operation""(Id),
                CONSTRAINT ""FK_OperInstance_TaskInstance"" FOREIGN KEY(""TaskInstanceId"")
                REFERENCES ""TaskInstance""(Id));
				CLUSTER ""OperInstance"" USING ""PK_OperInstance_Id"";
                CREATE INDEX IF NOT EXISTS ""idx_OperInstance_OperationId"" ON ""OperInstance""(""OperationId"" ASC);
                CREATE INDEX IF NOT EXISTS ""idx_OperInstance_TaskInstanceId"" ON ""OperInstance""(""TaskInstanceId"" ASC);");
        } //database structure creating
    }
}