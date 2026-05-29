namespace Skyline.DataMiner.Utils.Protocol.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Scripting;

    /// <summary>
    /// Extension of the SLProtocol class.
    /// </summary>
    public static class ProtocolExtension
    {
        /// <summary>
        /// Removes the rows with the specified primary keys from the specified table.
        /// </summary>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tablePid">The ID of the table parameter.</param>
        /// <param name="keysToDelete">The primary keys of the rows to remove.</param>
        /// <exception cref="ArgumentNullException"><paramref name="keysToDelete"/> is <see langword="null"/>.</exception>
        public static void DeleteRows(this SLProtocol protocol, int tablePid, IEnumerable<object> keysToDelete)
        {
            // Sanity checks
            if (keysToDelete == null)
                throw new ArgumentNullException(nameof(keysToDelete));

            var keysToDeleteArray = keysToDelete.ToArray();

            if (keysToDeleteArray.Length == 0)
            {
                // No rows to delete
                return;
            }

            // Build delete row object
            string[] deleteRowKeys = new string[keysToDeleteArray.Length];
            for (int i = 0; i < deleteRowKeys.Length; i++)
            {
                deleteRowKeys[i] = (string)keysToDeleteArray[i];
            }

            // Delete rows
            protocol.NotifyProtocol(156, tablePid, deleteRowKeys);
        }

        /// <summary>
        /// Removes the rows with the specified primary keys from the specified table.
        /// </summary>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tablePid">The ID of the table parameter.</param>
        /// <param name="keysToDelete">The primary keys of the rows to remove.</param>
        /// <exception cref="ArgumentNullException"><paramref name="keysToDelete"/> is <see langword="null"/>.</exception>
        public static void DeleteRows(this SLProtocol protocol, int tablePid, IEnumerable<string> keysToDelete)
        {
            // Sanity checks
            if (keysToDelete == null)
                throw new ArgumentNullException(nameof(keysToDelete));

            var deleteRowKeys = keysToDelete.ToArray();

            if (deleteRowKeys.Length == 0)
            {
                // No rows to delete
                return;
            }

            // Delete rows
            protocol.NotifyProtocol(156, tablePid, deleteRowKeys);
        }

        /// <summary>
        /// Retrieves a cell from a table with the specified <paramref name="tablePid"/>, <paramref name="rowPK"/> and the <paramref name="columnIdx"/>.
        /// </summary>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tablePid">The ID of the table parameter.</param>
        /// <param name="rowPK">The primary key of the row.</param>
        /// <param name="columnIdx">The 0-based position of the column, corresponding to the idx as defined in protocol.xml file.</param>
        /// <returns>The value of the cell.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="rowPK"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <list type="bullet">
        /// <para>The tablePid can be retrieved with the static Parameter class.</para>
        /// <para>The columnIdx can be retrieved with the static Parameter.[table].Idx class.</para>
        /// <para>returns <see langword="null"/> for uninitialized cells.</para>
        /// </list>
        /// </remarks>
        public static object GetCell(this SLProtocol protocol, int tablePid, string rowPK, int columnIdx)
        {
            if (String.IsNullOrEmpty(rowPK))
                throw new ArgumentNullException(nameof(rowPK));

            return protocol.NotifyProtocol(122, new object[] { tablePid, rowPK, columnIdx + 1 }, null);
        }

        /// <summary>
        /// Retrieves the values of the column with the specified <paramref name="tablePid"/> and <paramref name="columnIdx"/>.
        /// </summary>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tablePid">The ID of the table parameter.</param>
        /// <param name="columnIdx">The 0-based position of the column, corresponding to the idx as defined in protocol.xml file.</param>
        /// <returns>The values of the retrieved column.</returns>
        public static object[] GetColumn(this SLProtocol protocol, int tablePid, uint columnIdx)
        {
            var columns = protocol.GetColumns(tablePid, new uint[] { columnIdx });
            return (object[])columns[0];
        }

        /// <summary>
        /// Retrieves the values of the columns with the specified <paramref name="tablePid"/> and <paramref name="columnsIdx"/>.
        /// </summary>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tablePid">The ID of the table parameter.</param>
        /// <param name="columnsIdx">The 0-based positions of the columns, corresponding to the idx as defined in protocol.xml file.</param>
        /// <returns>The values of the retrieved columns.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="columnsIdx"/> is <see langword="null"/>.</exception>
        public static object[] GetColumns(this SLProtocol protocol, int tablePid, IEnumerable<uint> columnsIdx)
        {
            // Sanity checks
            if (columnsIdx == null)
                throw new ArgumentNullException(nameof(columnsIdx));

            var columnsIdxArray = columnsIdx.ToArray();

            if (columnsIdxArray.Length == 0)
            {
                return new object[] { };
            }

            return (object[])protocol.NotifyProtocol(321, tablePid, columnsIdxArray);
        }

        /// <summary>
        /// Retrieves the values of the columns with the specified <paramref name="tablePid"/> and <paramref name="columnsIdx"/>.
        /// </summary>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tablePid">The ID of the table parameter.</param>
        /// <param name="keyIdx">The 0-based position of the key column, corresponding to the idx as defined in protocol.xml file. The values of this column will be the key of the resulting dictionary.</param>
        /// <param name="columnsIdx">The 0-based positions of the columns, corresponding to the idx as defined in protocol.xml file.</param>
        /// <returns>
        /// A dictionary mapping the primary key to an array of column values, for each row in the table.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="columnsIdx"/> is <see langword="null"/>.</exception>
        public static IDictionary<string, object[]> GetColumns(this SLProtocol protocol, int tablePid, uint keyIdx, IEnumerable<uint> columnsIdx)
        {
            // Sanity checks
            if (protocol is null)
            {
                throw new ArgumentNullException(nameof(protocol));
            }

            if (columnsIdx == null)
            {
                throw new ArgumentNullException(nameof(columnsIdx));
            }

            // Ensure we retrieve the key column
            var columnsToRetrieve = columnsIdx.ToList();
            var originalColumnsCount = columnsToRetrieve.Count;

            if (!columnsToRetrieve.Contains(keyIdx))
            {
                columnsToRetrieve.Add(keyIdx);
            }

            // Retrieve the columns.
            var columns = protocol.GetColumns(tablePid, columnsToRetrieve);

            // Find the key column index
            int keyColumnIndex = columnsToRetrieve.IndexOf(keyIdx);
            var keyColumn = (object[])columns[keyColumnIndex];

            // Build the result.
            var result = new Dictionary<string, object[]>();

            for (var rowIndex = 0; rowIndex < keyColumn.Length; rowIndex++)
            {
                var key = Convert.ToString(keyColumn[rowIndex]);
                var rowValues = new object[originalColumnsCount];

                for (var colIndex = 0; colIndex < originalColumnsCount; colIndex++)
                {
                    var columnData = (object[])columns[colIndex];
                    rowValues[colIndex] = rowIndex < columnData.Length ? columnData[rowIndex] : null;
                }

                result[key] = rowValues;
            }

            return result;
        }

        /// <summary>
		/// Gets a column with the desired format.
		/// </summary>
		/// <typeparam name="T">Type of the Column.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tableId">Id of the table to fetch the column from.</param>
		/// <param name="columnIdx">Index of the desired column.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> with the desired column.</returns>
        public static IEnumerable<T> GetColumn<T>(this SLProtocol protocol, int tableId, uint columnIdx)
            where T : IConvertible
        {
            var column = (object[])((object[])protocol.NotifyProtocol(321, tableId, new[] { columnIdx }))[0];

            for (var i = 0; i < column.Length; i++)
            {
                yield return column[i].ChangeType<T>();
            }
        }

        /// <summary>
		/// Gets two columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices, Func<T1, T2, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
        {
            const int columnCount = 2;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>());
            }
        }

        /// <summary>
		/// Gets three columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices, Func<T1, T2, T3, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
        {
            const int columnCount = 3;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>());
            }
        }

        /// <summary>
		/// Gets four columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices, Func<T1, T2, T3, T4, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
        {
            const int columnCount = 4;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>());
            }
        }

        /// <summary>
		/// Gets five columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="T5">Type of the fifth Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, T5, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices, Func<T1, T2, T3, T4, T5, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
        {
            const int columnCount = 5;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>(),
                    ((object[])columns[4])[i].ChangeType<T5>());
            }
        }

        /// <summary>
		/// Gets six columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="T5">Type of the fifth Column.</typeparam>
		/// <typeparam name="T6">Type of the sixth Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, T5, T6, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices,
            Func<T1, T2, T3, T4, T5, T6, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
        {
            const int columnCount = 6;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>(),
                    ((object[])columns[4])[i].ChangeType<T5>(),
                    ((object[])columns[5])[i].ChangeType<T6>());
            }
        }

        /// <summary>
		/// Gets seven columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="T5">Type of the fifth Column.</typeparam>
		/// <typeparam name="T6">Type of the sixth Column.</typeparam>
		/// <typeparam name="T7">Type of the seventh Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, T5, T6, T7, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices,
            Func<T1, T2, T3, T4, T5, T6, T7, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
        {
            const int columnCount = 7;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>(),
                    ((object[])columns[4])[i].ChangeType<T5>(),
                    ((object[])columns[5])[i].ChangeType<T6>(),
                    ((object[])columns[6])[i].ChangeType<T7>());
            }
        }

        /// <summary>
		/// Gets eight columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="T5">Type of the fifth Column.</typeparam>
		/// <typeparam name="T6">Type of the sixth Column.</typeparam>
		/// <typeparam name="T7">Type of the seventh Column.</typeparam>
		/// <typeparam name="T8">Type of the eighth Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, T5, T6, T7, T8, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
            where T8 : IConvertible
        {
            const int columnCount = 8;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>(),
                    ((object[])columns[4])[i].ChangeType<T5>(),
                    ((object[])columns[5])[i].ChangeType<T6>(),
                    ((object[])columns[6])[i].ChangeType<T7>(),
                    ((object[])columns[7])[i].ChangeType<T8>());
            }
        }

        /// <summary>
		/// Gets nine columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="T5">Type of the fifth Column.</typeparam>
		/// <typeparam name="T6">Type of the sixth Column.</typeparam>
		/// <typeparam name="T7">Type of the seventh Column.</typeparam>
		/// <typeparam name="T8">Type of the eighth Column.</typeparam>
		/// <typeparam name="T9">Type of the ninth Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
            where T8 : IConvertible
            where T9 : IConvertible
        {
            const int columnCount = 9;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>(),
                    ((object[])columns[4])[i].ChangeType<T5>(),
                    ((object[])columns[5])[i].ChangeType<T6>(),
                    ((object[])columns[6])[i].ChangeType<T7>(),
                    ((object[])columns[7])[i].ChangeType<T8>(),
                    ((object[])columns[8])[i].ChangeType<T9>());
            }
        }

        /// <summary>
		/// Gets ten columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="T5">Type of the fifth Column.</typeparam>
		/// <typeparam name="T6">Type of the sixth Column.</typeparam>
		/// <typeparam name="T7">Type of the seventh Column.</typeparam>
		/// <typeparam name="T8">Type of the eighth Column.</typeparam>
		/// <typeparam name="T9">Type of the ninth Column.</typeparam>
		/// <typeparam name="T10">Type of the tenth Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
		/// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
            where T8 : IConvertible
            where T9 : IConvertible
            where T10 : IConvertible
        {
            const int columnCount = 10;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>(),
                    ((object[])columns[4])[i].ChangeType<T5>(),
                    ((object[])columns[5])[i].ChangeType<T6>(),
                    ((object[])columns[6])[i].ChangeType<T7>(),
                    ((object[])columns[7])[i].ChangeType<T8>(),
                    ((object[])columns[8])[i].ChangeType<T9>(),
                    ((object[])columns[9])[i].ChangeType<T10>());
            }
        }

        /// <summary>
		/// Gets eleven columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="T5">Type of the fifth Column.</typeparam>
		/// <typeparam name="T6">Type of the sixth Column.</typeparam>
		/// <typeparam name="T7">Type of the seventh Column.</typeparam>
		/// <typeparam name="T8">Type of the eighth Column.</typeparam>
		/// <typeparam name="T9">Type of the ninth Column.</typeparam>
		/// <typeparam name="T10">Type of the tenth Column.</typeparam>
		/// <typeparam name="T11">Type of the eleventh Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
            where T8 : IConvertible
            where T9 : IConvertible
            where T10 : IConvertible
            where T11 : IConvertible
        {
            const int columnCount = 11;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>(),
                    ((object[])columns[4])[i].ChangeType<T5>(),
                    ((object[])columns[5])[i].ChangeType<T6>(),
                    ((object[])columns[6])[i].ChangeType<T7>(),
                    ((object[])columns[7])[i].ChangeType<T8>(),
                    ((object[])columns[8])[i].ChangeType<T9>(),
                    ((object[])columns[9])[i].ChangeType<T10>(),
                    ((object[])columns[10])[i].ChangeType<T11>());
            }
        }

        /// <summary>
		/// Gets twelve columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="T5">Type of the fifth Column.</typeparam>
		/// <typeparam name="T6">Type of the sixth Column.</typeparam>
		/// <typeparam name="T7">Type of the seventh Column.</typeparam>
		/// <typeparam name="T8">Type of the eighth Column.</typeparam>
		/// <typeparam name="T9">Type of the ninth Column.</typeparam>
		/// <typeparam name="T10">Type of the tenth Column.</typeparam>
		/// <typeparam name="T11">Type of the eleventh Column.</typeparam>
		/// <typeparam name="T12">Type of the twelfth Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
		/// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
            where T8 : IConvertible
            where T9 : IConvertible
            where T10 : IConvertible
            where T11 : IConvertible
            where T12 : IConvertible
        {
            const int columnCount = 12;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>(),
                    ((object[])columns[4])[i].ChangeType<T5>(),
                    ((object[])columns[5])[i].ChangeType<T6>(),
                    ((object[])columns[6])[i].ChangeType<T7>(),
                    ((object[])columns[7])[i].ChangeType<T8>(),
                    ((object[])columns[8])[i].ChangeType<T9>(),
                    ((object[])columns[9])[i].ChangeType<T10>(),
                    ((object[])columns[10])[i].ChangeType<T11>(),
                    ((object[])columns[11])[i].ChangeType<T12>());
            }
        }

        /// <summary>
		/// Gets thirteen columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="T5">Type of the fifth Column.</typeparam>
		/// <typeparam name="T6">Type of the sixth Column.</typeparam>
		/// <typeparam name="T7">Type of the seventh Column.</typeparam>
		/// <typeparam name="T8">Type of the eighth Column.</typeparam>
		/// <typeparam name="T9">Type of the ninth Column.</typeparam>
		/// <typeparam name="T10">Type of the tenth Column.</typeparam>
		/// <typeparam name="T11">Type of the eleventh Column.</typeparam>
		/// <typeparam name="T12">Type of the twelfth Column.</typeparam>
		/// <typeparam name="T13">Type of the thirteenth Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
            where T8 : IConvertible
            where T9 : IConvertible
            where T10 : IConvertible
            where T11 : IConvertible
            where T12 : IConvertible
            where T13 : IConvertible
        {
            const int columnCount = 13;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>(),
                    ((object[])columns[4])[i].ChangeType<T5>(),
                    ((object[])columns[5])[i].ChangeType<T6>(),
                    ((object[])columns[6])[i].ChangeType<T7>(),
                    ((object[])columns[7])[i].ChangeType<T8>(),
                    ((object[])columns[8])[i].ChangeType<T9>(),
                    ((object[])columns[9])[i].ChangeType<T10>(),
                    ((object[])columns[10])[i].ChangeType<T11>(),
                    ((object[])columns[11])[i].ChangeType<T12>(),
                    ((object[])columns[12])[i].ChangeType<T13>());
            }
        }

        /// <summary>
		/// Gets fourteen columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="T5">Type of the fifth Column.</typeparam>
		/// <typeparam name="T6">Type of the sixth Column.</typeparam>
		/// <typeparam name="T7">Type of the seventh Column.</typeparam>
		/// <typeparam name="T8">Type of the eighth Column.</typeparam>
		/// <typeparam name="T9">Type of the ninth Column.</typeparam>
		/// <typeparam name="T10">Type of the tenth Column.</typeparam>
		/// <typeparam name="T11">Type of the eleventh Column.</typeparam>
		/// <typeparam name="T12">Type of the twelfth Column.</typeparam>
		/// <typeparam name="T13">Type of the thirteenth Column.</typeparam>
		/// <typeparam name="T14">Type of the fourteenth Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
		/// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
            where T8 : IConvertible
            where T9 : IConvertible
            where T10 : IConvertible
            where T11 : IConvertible
            where T12 : IConvertible
            where T13 : IConvertible
            where T14 : IConvertible
        {
            const int columnCount = 14;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>(),
                    ((object[])columns[4])[i].ChangeType<T5>(),
                    ((object[])columns[5])[i].ChangeType<T6>(),
                    ((object[])columns[6])[i].ChangeType<T7>(),
                    ((object[])columns[7])[i].ChangeType<T8>(),
                    ((object[])columns[8])[i].ChangeType<T9>(),
                    ((object[])columns[9])[i].ChangeType<T10>(),
                    ((object[])columns[10])[i].ChangeType<T11>(),
                    ((object[])columns[11])[i].ChangeType<T12>(),
                    ((object[])columns[12])[i].ChangeType<T13>(),
                    ((object[])columns[13])[i].ChangeType<T14>());
            }
        }

        /// <summary>
		/// Gets fifteen columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="T5">Type of the fifth Column.</typeparam>
		/// <typeparam name="T6">Type of the sixth Column.</typeparam>
		/// <typeparam name="T7">Type of the seventh Column.</typeparam>
		/// <typeparam name="T8">Type of the eighth Column.</typeparam>
		/// <typeparam name="T9">Type of the ninth Column.</typeparam>
		/// <typeparam name="T10">Type of the tenth Column.</typeparam>
		/// <typeparam name="T11">Type of the eleventh Column.</typeparam>
		/// <typeparam name="T12">Type of the twelfth Column.</typeparam>
		/// <typeparam name="T13">Type of the thirteenth Column.</typeparam>
		/// <typeparam name="T14">Type of the fourteenth Column.</typeparam>
		/// <typeparam name="T15">Type of the fifteenth Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
            where T8 : IConvertible
            where T9 : IConvertible
            where T10 : IConvertible
            where T11 : IConvertible
            where T12 : IConvertible
            where T13 : IConvertible
            where T14 : IConvertible
            where T15 : IConvertible
        {
            const int columnCount = 15;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>(),
                    ((object[])columns[4])[i].ChangeType<T5>(),
                    ((object[])columns[5])[i].ChangeType<T6>(),
                    ((object[])columns[6])[i].ChangeType<T7>(),
                    ((object[])columns[7])[i].ChangeType<T8>(),
                    ((object[])columns[8])[i].ChangeType<T9>(),
                    ((object[])columns[9])[i].ChangeType<T10>(),
                    ((object[])columns[10])[i].ChangeType<T11>(),
                    ((object[])columns[11])[i].ChangeType<T12>(),
                    ((object[])columns[12])[i].ChangeType<T13>(),
                    ((object[])columns[13])[i].ChangeType<T14>(),
                    ((object[])columns[14])[i].ChangeType<T15>());
            }
        }

        /// <summary>
		/// Gets sixteen columns from a table and returns an array with the given selector.
		/// </summary>
		/// <typeparam name="T1">Type of the first Column.</typeparam>
		/// <typeparam name="T2">Type of the second Column.</typeparam>
		/// <typeparam name="T3">Type of the third Column.</typeparam>
		/// <typeparam name="T4">Type of the fourth Column.</typeparam>
		/// <typeparam name="T5">Type of the fifth Column.</typeparam>
		/// <typeparam name="T6">Type of the sixth Column.</typeparam>
		/// <typeparam name="T7">Type of the seventh Column.</typeparam>
		/// <typeparam name="T8">Type of the eighth Column.</typeparam>
		/// <typeparam name="T9">Type of the ninth Column.</typeparam>
		/// <typeparam name="T10">Type of the tenth Column.</typeparam>
		/// <typeparam name="T11">Type of the eleventh Column.</typeparam>
		/// <typeparam name="T12">Type of the twelfth Column.</typeparam>
		/// <typeparam name="T13">Type of the thirteenth Column.</typeparam>
		/// <typeparam name="T14">Type of the fourteenth Column.</typeparam>
		/// <typeparam name="T15">Type of the fifteenth Column.</typeparam>
		/// <typeparam name="T16">Type of the sixteenth Column.</typeparam>
		/// <typeparam name="TReturn">Type of the return value.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tableId">Id of the table to fetch the columns from.</param>
		/// <param name="columnIndices">Array with the Columns Indexes.</param>
		/// <param name="returnSelector">A function to map each column element to a return element.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TReturn"/> with the desired columns.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Number of columns doesn't match the number of returned members.
		/// </exception>
        public static IEnumerable<TReturn> GetColumns<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn>(this SLProtocol protocol, int tableId, uint[] columnIndices,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn> returnSelector)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
            where T8 : IConvertible
            where T9 : IConvertible
            where T10 : IConvertible
            where T11 : IConvertible
            where T12 : IConvertible
            where T13 : IConvertible
            where T14 : IConvertible
            where T15 : IConvertible
            where T16 : IConvertible
        {
            const int columnCount = 16;

            if (columnIndices == null)
            {
                throw new ArgumentNullException(nameof(columnIndices));
            }

            if (returnSelector == null)
            {
                throw new ArgumentNullException(nameof(returnSelector));
            }

            if (columnIndices.Length != columnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndices), $"Number of columns has to be {columnCount}");
            }

            var columns = (object[])protocol.NotifyProtocol(321, tableId, columnIndices);

            for (var i = 0; i < ((object[])columns[0]).Length; i++)
            {
                yield return returnSelector(
                    ((object[])columns[0])[i].ChangeType<T1>(),
                    ((object[])columns[1])[i].ChangeType<T2>(),
                    ((object[])columns[2])[i].ChangeType<T3>(),
                    ((object[])columns[3])[i].ChangeType<T4>(),
                    ((object[])columns[4])[i].ChangeType<T5>(),
                    ((object[])columns[5])[i].ChangeType<T6>(),
                    ((object[])columns[6])[i].ChangeType<T7>(),
                    ((object[])columns[7])[i].ChangeType<T8>(),
                    ((object[])columns[8])[i].ChangeType<T9>(),
                    ((object[])columns[9])[i].ChangeType<T10>(),
                    ((object[])columns[10])[i].ChangeType<T11>(),
                    ((object[])columns[11])[i].ChangeType<T12>(),
                    ((object[])columns[12])[i].ChangeType<T13>(),
                    ((object[])columns[13])[i].ChangeType<T14>(),
                    ((object[])columns[14])[i].ChangeType<T15>(),
                    ((object[])columns[15])[i].ChangeType<T16>());
            }
        }

        /// <summary>
		/// Executes a <see cref="SLProtocol.GetParameter(int)"/> and return the value in the desired format.
		/// </summary>
		/// <typeparam name="T">Type of the Parameter.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="paramId">Id of the parameter to retrieve.</param>
		/// <returns>The parameter value.</returns>
        public static T GetParameter<T>(this SLProtocol protocol, int paramId)
            where T : IConvertible
        {
            return protocol.GetParameter(paramId).ChangeType<T>();
        }

        /// <summary>
		/// Gets the desired parameters and converts to the given types.
		/// </summary>
		/// <typeparam name="T1">Type of the fist parameter.</typeparam>
		/// <typeparam name="T2">Type of the second parameter.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="paramIds">Array with the ids of the Parameters to fetch.</param>
		/// <param name="param1">Out variable with the first parameter value.</param>
		/// <param name="param2">Out variable with the second parameter value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="paramIds"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// The length of <paramref name="paramIds"/> differs from the number of out parameters.
		/// </exception>
        public static void GetParameters<T1, T2>(this SLProtocol protocol, uint[] paramIds, out T1 param1, out T2 param2)
            where T1 : IConvertible
            where T2 : IConvertible
        {
            if (paramIds is null)
            {
                throw new ArgumentNullException(nameof(paramIds));
            }

            if (paramIds.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(paramIds), "paramIds need to have the same length as the number of out parameters");
            }

            object[] parameters = (object[])protocol.GetParameters(paramIds);

            param1 = parameters[0].ChangeType<T1>();
            param2 = parameters[1].ChangeType<T2>();
        }

        /// <summary>
		/// Gets the desired parameters and converts to the given types.
		/// </summary>
		/// <typeparam name="T1">Type of the fist parameter.</typeparam>
		/// <typeparam name="T2">Type of the second parameter.</typeparam>
		/// <typeparam name="T3">Type of the third parameter.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="paramIds">Array with the ids of the Parameters to fetch.</param>
		/// <param name="param1">Out variable with the first parameter value.</param>
		/// <param name="param2">Out variable with the second parameter value.</param>
		/// <param name="param3">Out variable with the third parameter value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="paramIds"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// The length of <paramref name="paramIds"/> differs from the number of out parameters.
		/// </exception>
        public static void GetParameters<T1, T2, T3>(this SLProtocol protocol, uint[] paramIds, out T1 param1, out T2 param2, out T3 param3)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
        {
            if (paramIds is null)
            {
                throw new ArgumentNullException(nameof(paramIds));
            }

            if (paramIds.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(paramIds), "paramIds need to have the same length as the number of out parameters");
            }

            object[] parameters = (object[])protocol.GetParameters(paramIds);

            param1 = parameters[0].ChangeType<T1>();
            param2 = parameters[1].ChangeType<T2>();
            param3 = parameters[2].ChangeType<T3>();
        }

        /// <summary>
		/// Gets the desired parameters and converts to the given types.
		/// </summary>
		/// <typeparam name="T1">Type of the fist parameter.</typeparam>
		/// <typeparam name="T2">Type of the second parameter.</typeparam>
		/// <typeparam name="T3">Type of the third parameter.</typeparam>
		/// <typeparam name="T4">Type of the fourth parameter.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="paramIds">Array with the ids of the Parameters to fetch.</param>
		/// <param name="param1">Out variable with the first parameter value.</param>
		/// <param name="param2">Out variable with the second parameter value.</param>
		/// <param name="param3">Out variable with the third parameter value.</param>
		/// <param name="param4">Out variable with the fourth parameter value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="paramIds"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// The length of <paramref name="paramIds"/> differs from the number of out parameters.
		/// </exception>
        public static void GetParameters<T1, T2, T3, T4>(this SLProtocol protocol, uint[] paramIds, out T1 param1, out T2 param2, out T3 param3, out T4 param4)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
        {
            if (paramIds is null)
            {
                throw new ArgumentNullException(nameof(paramIds));
            }

            if (paramIds.Length != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(paramIds), "paramIds need to have the same length as the number of out parameters");
            }

            object[] parameters = (object[])protocol.GetParameters(paramIds);

            param1 = parameters[0].ChangeType<T1>();
            param2 = parameters[1].ChangeType<T2>();
            param3 = parameters[2].ChangeType<T3>();
            param4 = parameters[3].ChangeType<T4>();
        }

        /// <summary>
		/// Gets the desired parameters and converts to the given types.
		/// </summary>
		/// <typeparam name="T1">Type of the fist parameter.</typeparam>
		/// <typeparam name="T2">Type of the second parameter.</typeparam>
		/// <typeparam name="T3">Type of the third parameter.</typeparam>
		/// <typeparam name="T4">Type of the fourth parameter.</typeparam>
		/// <typeparam name="T5">Type of the fifth parameter.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="paramIds">Array with the ids of the Parameters to fetch.</param>
		/// <param name="param1">Out variable with the first parameter value.</param>
		/// <param name="param2">Out variable with the second parameter value.</param>
		/// <param name="param3">Out variable with the third parameter value.</param>
		/// <param name="param4">Out variable with the fourth parameter value.</param>
		/// <param name="param5">Out variable with the fifth parameter value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="paramIds"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// The length of <paramref name="paramIds"/> differs from the number of out parameters.
		/// </exception>
        public static void GetParameters<T1, T2, T3, T4, T5>(this SLProtocol protocol, uint[] paramIds, out T1 param1, out T2 param2, out T3 param3, out T4 param4, out T5 param5)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
        {
            if (paramIds is null)
            {
                throw new ArgumentNullException(nameof(paramIds));
            }

            if (paramIds.Length != 5)
            {
                throw new ArgumentOutOfRangeException(nameof(paramIds), "paramIds need to have the same length as the number of out parameters");
            }

            object[] parameters = (object[])protocol.GetParameters(paramIds);

            param1 = parameters[0].ChangeType<T1>();
            param2 = parameters[1].ChangeType<T2>();
            param3 = parameters[2].ChangeType<T3>();
            param4 = parameters[3].ChangeType<T4>();
            param5 = parameters[4].ChangeType<T5>();
        }

        /// <summary>
		/// Gets the desired parameters and converts to the given types.
		/// </summary>
		/// <typeparam name="T1">Type of the fist parameter.</typeparam>
		/// <typeparam name="T2">Type of the second parameter.</typeparam>
		/// <typeparam name="T3">Type of the third parameter.</typeparam>
		/// <typeparam name="T4">Type of the fourth parameter.</typeparam>
		/// <typeparam name="T5">Type of the fifth parameter.</typeparam>
		/// <typeparam name="T6">Type of the sixth parameter.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="paramIds">Array with the ids of the Parameters to fetch.</param>
		/// <param name="param1">Out variable with the first parameter value.</param>
		/// <param name="param2">Out variable with the second parameter value.</param>
		/// <param name="param3">Out variable with the third parameter value.</param>
		/// <param name="param4">Out variable with the fourth parameter value.</param>
		/// <param name="param5">Out variable with the fifth parameter value.</param>
		/// <param name="param6">Out variable with the sixth parameter value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="paramIds"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// The length of <paramref name="paramIds"/> differs from the number of out parameters.
		/// </exception>
        public static void GetParameters<T1, T2, T3, T4, T5, T6>(this SLProtocol protocol, uint[] paramIds, out T1 param1, out T2 param2, out T3 param3, out T4 param4, out T5 param5, out T6 param6)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
        {
            if (paramIds is null)
            {
                throw new ArgumentNullException(nameof(paramIds));
            }

            if (paramIds.Length != 6)
            {
                throw new ArgumentOutOfRangeException(nameof(paramIds), "paramIds need to have the same length as the number of out parameters");
            }

            object[] parameters = (object[])protocol.GetParameters(paramIds);

            param1 = parameters[0].ChangeType<T1>();
            param2 = parameters[1].ChangeType<T2>();
            param3 = parameters[2].ChangeType<T3>();
            param4 = parameters[3].ChangeType<T4>();
            param5 = parameters[4].ChangeType<T5>();
            param6 = parameters[5].ChangeType<T6>();
        }

        /// <summary>
		/// Gets the desired parameters and converts to the given types.
		/// </summary>
		/// <typeparam name="T1">Type of the fist parameter.</typeparam>
		/// <typeparam name="T2">Type of the second parameter.</typeparam>
		/// <typeparam name="T3">Type of the third parameter.</typeparam>
		/// <typeparam name="T4">Type of the fourth parameter.</typeparam>
		/// <typeparam name="T5">Type of the fifth parameter.</typeparam>
		/// <typeparam name="T6">Type of the sixth parameter.</typeparam>
		/// <typeparam name="T7">Type of the seventh parameter.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="paramIds">Array with the ids of the Parameters to fetch.</param>
		/// <param name="param1">Out variable with the first parameter value.</param>
		/// <param name="param2">Out variable with the second parameter value.</param>
		/// <param name="param3">Out variable with the third parameter value.</param>
		/// <param name="param4">Out variable with the fourth parameter value.</param>
		/// <param name="param5">Out variable with the fifth parameter value.</param>
		/// <param name="param6">Out variable with the sixth parameter value.</param>
		/// <param name="param7">Out variable with the seventh parameter value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="paramIds"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// The length of <paramref name="paramIds"/> differs from the number of out parameters.
		/// </exception>
        public static void GetParameters<T1, T2, T3, T4, T5, T6, T7>(
            this SLProtocol protocol,
            uint[] paramIds,
            out T1 param1,
            out T2 param2,
            out T3 param3,
            out T4 param4,
            out T5 param5,
            out T6 param6,
            out T7 param7)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
        {
            if (paramIds is null)
            {
                throw new ArgumentNullException(nameof(paramIds));
            }

            if (paramIds.Length != 7)
            {
                throw new ArgumentOutOfRangeException(nameof(paramIds), "paramIds need to have the same length as the number of out parameters");
            }

            object[] parameters = (object[])protocol.GetParameters(paramIds);

            param1 = parameters[0].ChangeType<T1>();
            param2 = parameters[1].ChangeType<T2>();
            param3 = parameters[2].ChangeType<T3>();
            param4 = parameters[3].ChangeType<T4>();
            param5 = parameters[4].ChangeType<T5>();
            param6 = parameters[5].ChangeType<T6>();
            param7 = parameters[6].ChangeType<T7>();
        }

        /// <summary>
		/// Gets the desired parameters and converts to the given types.
		/// </summary>
		/// <typeparam name="T1">Type of the fist parameter.</typeparam>
		/// <typeparam name="T2">Type of the second parameter.</typeparam>
		/// <typeparam name="T3">Type of the third parameter.</typeparam>
		/// <typeparam name="T4">Type of the fourth parameter.</typeparam>
		/// <typeparam name="T5">Type of the fifth parameter.</typeparam>
		/// <typeparam name="T6">Type of the sixth parameter.</typeparam>
		/// <typeparam name="T7">Type of the seventh parameter.</typeparam>
		/// <typeparam name="T8">Type of the eighth parameter.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="paramIds">Array with the ids of the Parameters to fetch.</param>
		/// <param name="param1">Out variable with the first parameter value.</param>
		/// <param name="param2">Out variable with the second parameter value.</param>
		/// <param name="param3">Out variable with the third parameter value.</param>
		/// <param name="param4">Out variable with the fourth parameter value.</param>
		/// <param name="param5">Out variable with the fifth parameter value.</param>
		/// <param name="param6">Out variable with the sixth parameter value.</param>
		/// <param name="param7">Out variable with the seventh parameter value.</param>
		/// <param name="param8">Out variable with the eighth parameter value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="paramIds"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// The length of <paramref name="paramIds"/> differs from the number of out parameters.
		/// </exception>
        public static void GetParameters<T1, T2, T3, T4, T5, T6, T7, T8>(
            this SLProtocol protocol,
            uint[] paramIds,
            out T1 param1,
            out T2 param2,
            out T3 param3,
            out T4 param4,
            out T5 param5,
            out T6 param6,
            out T7 param7,
            out T8 param8)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
            where T8 : IConvertible
        {
            if (paramIds is null)
            {
                throw new ArgumentNullException(nameof(paramIds));
            }

            if (paramIds.Length != 8)
            {
                throw new ArgumentOutOfRangeException(nameof(paramIds), "paramIds need to have the same length as the number of out parameters");
            }

            object[] parameters = (object[])protocol.GetParameters(paramIds);

            param1 = parameters[0].ChangeType<T1>();
            param2 = parameters[1].ChangeType<T2>();
            param3 = parameters[2].ChangeType<T3>();
            param4 = parameters[3].ChangeType<T4>();
            param5 = parameters[4].ChangeType<T5>();
            param6 = parameters[5].ChangeType<T6>();
            param7 = parameters[6].ChangeType<T7>();
            param8 = parameters[7].ChangeType<T8>();
        }

        /// <summary>
		/// Gets the desired parameters and converts to the given types.
		/// </summary>
		/// <typeparam name="T1">Type of the fist parameter.</typeparam>
		/// <typeparam name="T2">Type of the second parameter.</typeparam>
		/// <typeparam name="T3">Type of the third parameter.</typeparam>
		/// <typeparam name="T4">Type of the fourth parameter.</typeparam>
		/// <typeparam name="T5">Type of the fifth parameter.</typeparam>
		/// <typeparam name="T6">Type of the sixth parameter.</typeparam>
		/// <typeparam name="T7">Type of the seventh parameter.</typeparam>
		/// <typeparam name="T8">Type of the eighth parameter.</typeparam>
		/// <typeparam name="T9">Type of the ninth parameter.</typeparam>
        /// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="paramIds">Array with the ids of the Parameters to fetch.</param>
		/// <param name="param1">Out variable with the first parameter value.</param>
		/// <param name="param2">Out variable with the second parameter value.</param>
		/// <param name="param3">Out variable with the third parameter value.</param>
		/// <param name="param4">Out variable with the fourth parameter value.</param>
		/// <param name="param5">Out variable with the fifth parameter value.</param>
		/// <param name="param6">Out variable with the sixth parameter value.</param>
		/// <param name="param7">Out variable with the seventh parameter value.</param>
		/// <param name="param8">Out variable with the eighth parameter value.</param>
		/// <param name="param9">Out variable with the ninth parameter value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="paramIds"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// The length of <paramref name="paramIds"/> differs from the number of out parameters.
		/// </exception>
        public static void GetParameters<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            this SLProtocol protocol,
            uint[] paramIds,
            out T1 param1,
            out T2 param2,
            out T3 param3,
            out T4 param4,
            out T5 param5,
            out T6 param6,
            out T7 param7,
            out T8 param8,
            out T9 param9)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
            where T8 : IConvertible
            where T9 : IConvertible
        {
            if (paramIds is null)
            {
                throw new ArgumentNullException(nameof(paramIds));
            }

            if (paramIds.Length != 9)
            {
                throw new ArgumentOutOfRangeException(nameof(paramIds), "paramIds need to have the same length as the number of out parameters");
            }

            object[] parameters = (object[])protocol.GetParameters(paramIds);

            param1 = parameters[0].ChangeType<T1>();
            param2 = parameters[1].ChangeType<T2>();
            param3 = parameters[2].ChangeType<T3>();
            param4 = parameters[3].ChangeType<T4>();
            param5 = parameters[4].ChangeType<T5>();
            param6 = parameters[5].ChangeType<T6>();
            param7 = parameters[6].ChangeType<T7>();
            param8 = parameters[7].ChangeType<T8>();
            param9 = parameters[8].ChangeType<T9>();
        }

        /// <summary>
		/// Gets the desired parameters and converts to the given types.
		/// </summary>
		/// <typeparam name="T1">Type of the fist parameter.</typeparam>
		/// <typeparam name="T2">Type of the second parameter.</typeparam>
		/// <typeparam name="T3">Type of the third parameter.</typeparam>
		/// <typeparam name="T4">Type of the fourth parameter.</typeparam>
		/// <typeparam name="T5">Type of the fifth parameter.</typeparam>
		/// <typeparam name="T6">Type of the sixth parameter.</typeparam>
		/// <typeparam name="T7">Type of the seventh parameter.</typeparam>
		/// <typeparam name="T8">Type of the eighth parameter.</typeparam>
		/// <typeparam name="T9">Type of the ninth parameter.</typeparam>
		/// <typeparam name="T10">Type of the tenth parameter.</typeparam>
		/// <param name="paramIds">Array with the ids of the Parameters to fetch.</param>
		/// <param name="param1">Out variable with the first parameter value.</param>
		/// <param name="param2">Out variable with the second parameter value.</param>
		/// <param name="param3">Out variable with the third parameter value.</param>
		/// <param name="param4">Out variable with the fourth parameter value.</param>
		/// <param name="param5">Out variable with the fifth parameter value.</param>
		/// <param name="param6">Out variable with the sixth parameter value.</param>
		/// <param name="param7">Out variable with the seventh parameter value.</param>
		/// <param name="param8">Out variable with the eighth parameter value.</param>
		/// <param name="param9">Out variable with the ninth parameter value.</param>
		/// <param name="param10">Out variable with the tenth parameter value.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// If the length of the paramIds is different from the number of out parameters.
		/// </exception>
        public static void GetParameters<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            this SLProtocol protocol,
            uint[] paramIds,
            out T1 param1,
            out T2 param2,
            out T3 param3,
            out T4 param4,
            out T5 param5,
            out T6 param6,
            out T7 param7,
            out T8 param8,
            out T9 param9,
            out T10 param10)
            where T1 : IConvertible
            where T2 : IConvertible
            where T3 : IConvertible
            where T4 : IConvertible
            where T5 : IConvertible
            where T6 : IConvertible
            where T7 : IConvertible
            where T8 : IConvertible
            where T9 : IConvertible
            where T10 : IConvertible
        {
            if (paramIds is null)
            {
                throw new ArgumentNullException(nameof(paramIds));
            }

            if (paramIds.Length != 10)
            {
                throw new ArgumentOutOfRangeException(nameof(paramIds), "paramIds need to have the same length as the number of out parameters");
            }

            object[] parameters = (object[])protocol.GetParameters(paramIds);

            param1 = parameters[0].ChangeType<T1>();
            param2 = parameters[1].ChangeType<T2>();
            param3 = parameters[2].ChangeType<T3>();
            param4 = parameters[3].ChangeType<T4>();
            param5 = parameters[4].ChangeType<T5>();
            param6 = parameters[5].ChangeType<T6>();
            param7 = parameters[6].ChangeType<T7>();
            param8 = parameters[7].ChangeType<T8>();
            param9 = parameters[8].ChangeType<T9>();
            param10 = parameters[9].ChangeType<T10>();
        }

        /// <summary>
        /// Runs the specified action.
        /// </summary>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="actionId">the ID of the action.</param>
        public static void RunAction(this SLProtocol protocol, int actionId)
        {
            protocol.NotifyProtocol(221/*NT_RUN_ACTION*/, actionId, null);
        }

        /// <summary>
        /// Sets the value of a cell in a table, identified by the primary key of the row and column position, with the specified value.
        /// Use <see langword="null"/> as <paramref name="value"/> to clear the cell.
        /// The <paramref name="tablePid"/> can be retrieved with the static Parameter class.
        /// The <paramref name="columnIdx"/> can be retrieved with the static Parameter.[table].Idx class.
        /// </summary>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="tablePid">The ID of the table parameter.</param>
        /// <param name="rowPK">The primary key of the row.</param>
        /// <param name="columnIdx">The 0-based column position.</param>
        /// <param name="value">The new value. Use <see langword="null"/> to clear the cell.</param>
        /// <param name="dateTime">The time stamp for the new values (in case of historySets).</param>
        /// <returns>Whether the cell value has changed. <see langword="true"/> indicates change; otherwise, <see langword="false"/>.</returns>
        /// <remarks>The primary key can never be updated.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="rowPK"/> is <see langword="null"/>.</exception>
        public static bool SetCell(this SLProtocol protocol, int tablePid, string rowPK, int columnIdx, object value, DateTime? dateTime = null)
        {
            if (String.IsNullOrEmpty(rowPK))
                throw new ArgumentNullException(nameof(rowPK));

            if (dateTime == null)
            {
                return protocol.SetParameterIndexByKey(tablePid, rowPK, columnIdx + 1, value);
            }
            else
            {
                return protocol.SetParameterIndexByKey(tablePid, rowPK, columnIdx + 1, value, dateTime.Value);
            }
        }

        /* We (PedroD and SimonV) considered different options for columnsValues argument type of SetColumns :
         *      - object[][]							=> Efficient but not so flexible
         *      - IEnumerable<IEnumarable<object>>		=> Full flexibility but less efficient
         *      - IList<IEnumerable<object>>			=> Efficient and seems to bring decent flexibility. However, when actually trying to use it, the flexibility is not there.
         *                                                  For some reason, providing it with a list or so doesn't compile.
         *                                                  Only object[][] works. Has to do with the fact that IList do not supper covariance or contravariance because they need to support both reading and writing.
         *      - IReadOnlyList<IEnumerable<object>>	=> Efficient and brings decent flexibility.
         *                                                  This one works because IReadOnlyList is read-only meaning it can support covariance.
         * 
         *  => We opted for option 4 which seemed like a good middle-ground.
        */

        /////// <summary>
        /////// Sets the specified columns.
        /////// </summary>
        /////// <param name="protocol">Link with SLProtocol process.</param>
        /////// <param name="columnsPid">The column parameter ID of the columns to update. First item should contain the table PID. Primary key column PID should never be provided.</param>
        /////// <param name="columnsValues">The column values for each column to update. First item should contain the primary keys as <see cref="string" />.</param>
        /////// <param name="dateTime">The time stamp for the new values (in case of historySets).</param>
        /////// <exception cref="ArgumentNullException"><paramref name="columnsPid"/> or <paramref name="columnsValues"/> is <see langword="null"/>.</exception>
        ////public static void SetColumns(this SLProtocol protocol, int[] columnsPid, object[][] columnsValues, DateTime? dateTime = null)
        ////{
        ////    // Sanity checks
        ////    if (columnsPid == null)
        ////        throw new ArgumentNullException(nameof(columnsPid));

        ////    if (columnsValues == null)
        ////        throw new ArgumentNullException(nameof(columnsValues));

        ////    if (columnsPid.Length != columnsValues.Length)
        ////        throw new ArgumentException($"Length of {nameof(columnsPid)} '{columnsPid.Length}' != length of {nameof(columnsValues)} '{columnsValues.Length}'.");

        ////    // Prepare data
        ////    int columnsCount = columnsPid.Length;

        ////    object[] columnsPidArray = new object[columnsCount + 1];
        ////    object[] columnsValuesArray = new object[columnsCount];

        ////    for (int i = 0; i < columnsCount; i++)
        ////    {
        ////        columnsPidArray[i] = columnsPid[i];
        ////        columnsValuesArray[i] = columnsValues[i];
        ////    }

        ////    // Options (Clear & Leave, history sets)
        ////    object[] setColumnOptions = dateTime == null ? new object[] { true } : new object[] { true, dateTime.Value };
        ////    columnsPidArray[columnsCount] = setColumnOptions;

        ////    // Set columns
        ////    protocol.NotifyProtocol(220, columnsPidArray, columnsValuesArray);
        ////}

        /////// <summary>
        /////// Sets the specified columns.
        /////// </summary>
        /////// <param name="protocol">Link with SLProtocol process.</param>
        /////// <param name="columnsPid">The column parameter ID of the columns to update. First item should contain the table PID. Primary key column PID should never be provided.</param>
        /////// <param name="columnsValues">The column values for each column to update. First item should contain the primary keys as <see cref="string" />.</param>
        /////// <param name="dateTime">The time stamp for the new values (in case of historySets).</param>
        /////// <exception cref="ArgumentNullException"><paramref name="columnsPid"/> or <paramref name="columnsValues"/> is <see langword="null"/>.</exception>
        ////public static void SetColumns(this SLProtocol protocol, IEnumerable<int> columnsPid, IEnumerable<IEnumerable<object>> columnsValues, DateTime? dateTime = null)
        ////{
        ////    // Sanity checks
        ////    if (columnsPid == null)
        ////        throw new ArgumentNullException(nameof(columnsPid));

        ////    if (columnsValues == null)
        ////        throw new ArgumentNullException(nameof(columnsValues));

        ////    int columnsPidCount = columnsPid.Count();

        ////    if (columnsPidCount != columnsValues.Count())
        ////        throw new ArgumentException($"Length of {nameof(columnsPid)} '{columnsPidCount}' != length of {nameof(columnsValues)} '{columnsValues.Count()}'.");

        ////    // Prepare data
        ////    object[] columnsPidArray = new object[columnsPidCount + 1];
        ////    object[] columnsValuesArray = new object[columnsPidCount];

        ////    int columnPos = 0;
        ////    foreach (var columnPid in columnsPid)
        ////    {
        ////        columnsPidArray[columnPos++] = columnPid;
        ////    }

        ////    columnPos = 0;
        ////    foreach (var columnValue in columnsValues)
        ////    {
        ////        columnsValuesArray[columnPos++] = columnValue.ToArray();
        ////    }

        ////    // Options (Clear & Leave, history sets)
        ////    object[] setColumnOptions = dateTime == null ? new object[] { true } : new object[] { true, dateTime.Value };
        ////    columnsPidArray[columnsPidCount] = setColumnOptions;

        ////    // Set columns
        ////    protocol.NotifyProtocol(220, columnsPidArray, columnsValuesArray);
        ////}

        /////// <summary>
        /////// Sets the specified columns.
        /////// </summary>
        /////// <param name="protocol">Link with SLProtocol process.</param>
        /////// <param name="columnsPid">The column parameter ID of the columns to update. First item should contain the table PID. Primary key column PID should never be provided.</param>
        /////// <param name="columnsValues">The column values for each column to update. First item should contain the primary keys as <see cref="string" />.</param>
        /////// <param name="dateTime">The time stamp for the new values (in case of historySets).</param>
        /////// <exception cref="ArgumentNullException"><paramref name="columnsPid"/> or <paramref name="columnsValues"/> is <see langword="null"/>.</exception>
        ////public static void SetColumns(this SLProtocol protocol, IList<int> columnsPid, IList<IEnumerable<object>> columnsValues, DateTime? dateTime = null)
        ////{
        ////    // Sanity checks
        ////    if (columnsPid == null)
        ////        throw new ArgumentNullException(nameof(columnsPid));

        ////    if (columnsValues == null)
        ////        throw new ArgumentNullException(nameof(columnsValues));

        ////    if (columnsPid.Count != columnsValues.Count)
        ////        throw new ArgumentException($"Length of {nameof(columnsPid)} '{columnsPid.Count}' != length of {nameof(columnsValues)} '{columnsValues.Count}'.");

        ////    // Prepare data
        ////    int columnsCount = columnsPid.Count;

        ////    object[] columnsPidArray = new object[columnsCount + 1];
        ////    object[] columnsValuesArray = new object[columnsCount];

        ////    for (int i = 0; i < columnsCount; i++)
        ////    {
        ////        columnsPidArray[i] = columnsPid[i];
        ////        columnsValuesArray[i] = columnsValues[i].ToArray();
        ////    }

        ////    // Options (Clear & Leave, history sets)
        ////    object[] setColumnOptions = dateTime == null ? new object[] { true } : new object[] { true, dateTime.Value };
        ////    columnsPidArray[columnsCount] = setColumnOptions;

        ////    // Set columns
        ////    protocol.NotifyProtocol(220, columnsPidArray, columnsValuesArray);
        ////}

        /// <summary>
        /// Sets the specified columns.
        /// </summary>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="columnsPid">The column parameter ID of the columns to update. First item should contain the table PID. Primary key column PID should never be provided.</param>
        /// <param name="columnsValues">The column values for each column to update. First item should contain the primary keys as <see cref="string" />.</param>
        /// <param name="dateTime">The time stamp for the new values (in case of historySets).</param>
        /// <exception cref="ArgumentNullException"><paramref name="columnsPid"/> or <paramref name="columnsValues"/> is <see langword="null"/>.</exception>
        public static void SetColumns(this SLProtocol protocol, IList<int> columnsPid, IReadOnlyList<IEnumerable<object>> columnsValues, DateTime? dateTime = null)
        {
            // Sanity checks
            if (columnsPid == null)
                throw new ArgumentNullException(nameof(columnsPid));

            if (columnsValues == null)
                throw new ArgumentNullException(nameof(columnsValues));

            if (columnsPid.Count != columnsValues.Count)
                throw new ArgumentException($"Length of {nameof(columnsPid)} '{columnsPid.Count}' != length of {nameof(columnsValues)} '{columnsValues.Count}'.");

            // Prepare data
            int columnsCount = columnsPid.Count;

            object[] columnsPidArray = new object[columnsCount + 1];
            object[] columnsValuesArray = new object[columnsCount];

            for (int i = 0; i < columnsCount; i++)
            {
                columnsPidArray[i] = columnsPid[i];
                columnsValuesArray[i] = columnsValues[i].ToArray();
            }

            // Options (Clear & Leave, history sets)
            object[] setColumnOptions = dateTime == null ? new object[] { true } : new object[] { true, dateTime.Value };
            columnsPidArray[columnsCount] = setColumnOptions;

            // Set columns
            protocol.NotifyProtocol(220, columnsPidArray, columnsValuesArray);
        }

        /// <summary>
        /// Sets the specified columns.
        /// </summary>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="setColumnsData">The new column values per column PID. The first dictionary item should contain table PID as key and primary keys as value.</param>
        /// <param name="dateTime">The time stamp for the new values (in case of historySets).</param>
        /// <exception cref="ArgumentNullException"><paramref name="setColumnsData"/> is <see langword="null"/>.</exception>
        public static void SetColumns(this SLProtocol protocol, IDictionary<int, List<object>> setColumnsData, DateTime? dateTime = null)
        {
            // Sanity checks
            if (setColumnsData == null)
                throw new ArgumentNullException(nameof(setColumnsData));

            if (setColumnsData.Count == 0)
                return;

            int rowCount = setColumnsData.ElementAt(0).Value.Count;
            if (rowCount == 0)
            {
                // No rows to update
                return;
            }

            // Prepare data
            object[] setColumnPids = new object[setColumnsData.Count + 1];
            object[] setColumnValues = new object[setColumnsData.Count];

            int columnPos = 0;
            foreach (var setColumnData in setColumnsData)
            {
                // Sanity checks
                if (setColumnData.Value.Count != rowCount)
                {
                    protocol.Log(
                        $"QA{protocol.QActionID}|SetColumns|SetColumns on table '{setColumnsData.Keys.ToArray()[0]}' failed. " +
                            $"Not all columns contain the same number of rows.",
                        LogType.Error,
                        LogLevel.NoLogging);

                    return;
                }

                // Build set columns objects
                setColumnPids[columnPos] = setColumnData.Key;
                setColumnValues[columnPos] = setColumnData.Value.ToArray();

                columnPos++;
            }

            // Options (Clear & Leave, history sets)
            object[] setColumnOptions = dateTime == null ? new object[] { true } : new object[] { true, dateTime.Value };
            setColumnPids[setColumnPids.Length - 1] = setColumnOptions;

            // Set columns
            protocol.NotifyProtocol(220, setColumnPids, setColumnValues);
        }

        /// <summary>
        /// Sets the specified standalone parameters to the specified values.
        /// Note that due to the fact this method relies on a <c>Dictionary</c>, the order in which the sets are executed is not guaranteed.
        /// </summary>
        /// <param name="protocol">Link with SLProtocol process.</param>
        /// <param name="paramsToSet">The IDs of the standalone parameters to set with their value to set.</param>
        /// <param name="dateTime">The time stamp for the new values (in case of historySets).</param>
        /// <exception cref="ArgumentNullException"><paramref name="paramsToSet"/> is <see langword="null"/>.</exception>
        public static void SetParameters(this SLProtocol protocol, IDictionary<int, object> paramsToSet, DateTime? dateTime = null)
        {
            // Sanity checks
            if (paramsToSet == null)
                throw new ArgumentNullException(nameof(paramsToSet));

            if (paramsToSet.Count == 0)
                return;

            if (dateTime == null)
            {
                protocol.SetParameters(paramsToSet.Keys.ToArray(), paramsToSet.Values.ToArray());
            }
            else
            {
                DateTime[] historySetDates = new DateTime[paramsToSet.Count];
                for (int i = 0; i < historySetDates.Length; i++)
                {
                    historySetDates[i] = dateTime.Value;
                }

                protocol.SetParameters(paramsToSet.Keys.ToArray(), paramsToSet.Values.ToArray(), historySetDates);
            }
        }
    }
}
