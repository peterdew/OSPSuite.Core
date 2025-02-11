﻿using System;
using Microsoft.Extensions.Logging;
using OSPSuite.Core.Extensions;

namespace OSPSuite.Core.Services
{
   public static class LoggerExtensions
   {
      public static void AddError(this IOSPSuiteLogger logger, string message, string categoryName = null) => addToLog(logger, message, LogLevel.Error, categoryName);

      public static void AddError<T>(this IOSPSuiteLogger logger, string message) => addToLog<T>(logger, message, LogLevel.Error);

      public static void AddCriticalError(this IOSPSuiteLogger logger, string message, string categoryName = null) => logger.AddToLog(message, LogLevel.Critical, categoryName);

      public static void AddCriticalError<T>(this IOSPSuiteLogger logger, string message) => addToLog<T>(logger, message, LogLevel.Critical);

      public static void AddInfo(this IOSPSuiteLogger logger, string message, string categoryName = null) => logger.AddToLog(message, LogLevel.Information, categoryName);

      public static void AddInfo<T>(this IOSPSuiteLogger logger, string message) => addToLog<T>(logger, message, LogLevel.Information);

      public static void AddWarning(this IOSPSuiteLogger logger, string message, string categoryName = null) => logger.AddToLog(message, LogLevel.Warning, categoryName);

      public static void AddWarning<T>(this IOSPSuiteLogger logger, string message) => addToLog<T>(logger, message, LogLevel.Warning);

      public static void AddDebug(this IOSPSuiteLogger logger, string message, string categoryName = null) => logger.AddToLog(message, LogLevel.Debug, categoryName);

      public static void AddDebug<T>(this IOSPSuiteLogger logger, string message) => addToLog<T>(logger, message, LogLevel.Debug);

      public static void AddException(this IOSPSuiteLogger logger, Exception exception, string categoryName = null)
      {
         //Info message only => Should be shown as warning in log
         if (exception.IsInfoException())
            logger.AddWarning(exception.ExceptionMessage(addContactSupportInfo: false), categoryName);
         // Not an info message but an exception thrown by the suite. Error without stack trace
         else if (exception.IsOSPSuiteException())
            logger.AddError((exception.ExceptionMessage(addContactSupportInfo: false)), categoryName);
         // this is bad => Stack trace
         else
            logger.AddError(exception.ExceptionMessageWithStackTrace(false), categoryName);
      }

      public static void AddException<T>(this IOSPSuiteLogger logger, Exception exception) => logger.AddException(exception, typeof(T).Name);

      private static void addToLog(IOSPSuiteLogger logger, string message, LogLevel logLevel, string category)
      {
         logger?.AddToLog(message, logLevel, category);
      }

      private static void addToLog<T>(IOSPSuiteLogger logger, string message, LogLevel logLevel)
      {
         logger.AddToLog(message, logLevel, typeof(T).Name);
      }
   }
}