using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LogVisualizer.Domain;

namespace LogVisualizer.Domain
{

    /// <summary>
    /// The TimeLineTree class keeps a list of timestamps of events from a log file,
    /// and provides counts at each level of granularity, so that we can easily roll-up or
    /// drill-down.
    /// </summary>
    /// <remarks>
    /// Each successive level in the tree represents a more granular unit of time:
    /// Year -> Month -> Day -> ... -> milliseconds. The lowest level holds the offset into
    /// the source file at which the log entry can be found. 
    /// </remarks>
    public class TimelineTree
    {
        /// <summary>
        /// The list of top-level tree nodes that contain the values rolled up by year. 
        /// </summary>
        private readonly List<TimelineTreeNode> _yearNodes = new List<TimelineTreeNode>(2);


        public TimelineTree()
        {
        }

        /// <summary>
        /// Adds a new timestamp and offset to the tree, and updates all the intermediate counts. 
        /// </summary>
        /// <param name="timestamp">The timestamp of the log enttry.</param>
        /// <param name="offset">The log entry's position in the source file.</param>
        public void Add(DateTime timestamp, long offset)
        {
            var nodes = _yearNodes;

            foreach (TimeLineTreeLevel level in Enum.GetValues(typeof(TimeLineTreeLevel)))
            {
                var timePart = level.PartOf(timestamp);

                // Check that the timestamp has increased
                if (nodes.Count == 0 || nodes.Last().TimeComponent != timePart)
                {
                    // Most likely to be towards the end of the list, so search backwards. 
                    if (nodes.Count > 0 && nodes.Last().TimeComponent > timePart)
                    {
                        for (int i = nodes.Count - 2; i >= 0; --i)
                        {
                            var n = nodes[i];

                            if (n.TimeComponent > timePart)
                            {
                                continue;
                            }
                            else if (n.TimeComponent == timePart)
                            {
                                // just increment the count and move down to the next level
                                n.Count++;
                                break;
                            }
                            else
                            {
                                // Found the predecessor of the current entry - add the new
                                // node after it. 
                                var newNode = level == TimeLineTreeLevel.Millisecond
                                    ? (TimelineTreeNode)new LeafTimelineTreeNode(timePart, offset)
                                    : new InternalTimelineTreeNode(timePart);

                                nodes.Insert(i + 1, newNode);
                                break;
                            }
                        }
                    }
                    else
                    {
                        
                        if (level == TimeLineTreeLevel.Millisecond)
                        {
                            // At the lowest level of granularity, so add a leaf node
                            nodes.Add(new LeafTimelineTreeNode(timePart, offset));
                        }
                        else
                        {
                            // Add an internal node to the tree. 
                            nodes.Add(new InternalTimelineTreeNode(timePart));
                        }
                    }
                }
                else
                {
                    // Increment the count on the last entry. 
                    nodes.Last().Count++;
                }

                if (level != TimeLineTreeLevel.Millisecond)
                {
                    nodes = ((InternalTimelineTreeNode)nodes.Last()).Children;
                }

            }
        }

        /// <summary>
        /// The total number of events recorded in the tree. 
        /// </summary>
        public int Count { get => _yearNodes.Sum(x => x.Count); }


        /// <summary>
        /// Return a breakdown of the counts at the specified level.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="parentDate"></param>
        /// <returns></returns>
        public IEnumerable<TimeLineCount> CountsAtLevel(TimeLineTreeLevel level, DateTime? parentDate)
        {
            int year, month, day, hour, min, sec, millisec;

            TimeLineCount makeTimeLineCount(TimelineTreeNode node)
            {
                return new TimeLineCount(
                    new DateTime(year, month, day, hour, min, sec, millisec),
                    node.Count,
                    node.Offset);
            }

            // TODO: there must be a neater way of doing this!
            foreach (var yearNode in _yearNodes)
            {
                year = yearNode.TimeComponent;
                month = 1;
                day = 1;
                hour = 0;
                min = 0;
                sec = 0;
                millisec = 0;

                if (level == TimeLineTreeLevel.Year)
                {
                    yield return makeTimeLineCount(yearNode);
                    continue;
                }

                foreach (var monthNode in ((InternalTimelineTreeNode)yearNode).Children.Where(
                    x => !parentDate.HasValue || level == TimeLineTreeLevel.Month || parentDate.Value.Month == x.TimeComponent))
                {
                    month = monthNode.TimeComponent;

                    if (level == TimeLineTreeLevel.Month)
                    {
                        yield return makeTimeLineCount(monthNode);
                        continue;
                    }

                    foreach (var dayNode in ((InternalTimelineTreeNode)monthNode).Children.Where(
                        x => !parentDate.HasValue || level == TimeLineTreeLevel.Day || parentDate.Value.Day == x.TimeComponent))
                    {
                        day = dayNode.TimeComponent;

                        if (level == TimeLineTreeLevel.Day)
                        {
                            yield return makeTimeLineCount(dayNode);
                            continue;
                        }

                        foreach (var hourNode in ((InternalTimelineTreeNode)dayNode).Children.Where(
                            x => !parentDate.HasValue || level == TimeLineTreeLevel.Hour || parentDate.Value.Hour == x.TimeComponent))
                        {
                            hour = hourNode.TimeComponent;

                            if (level == TimeLineTreeLevel.Hour)
                            {
                                yield return makeTimeLineCount(hourNode);
                                continue;
                            }

                            foreach (var minNode in ((InternalTimelineTreeNode)hourNode).Children.Where(
                                x => !parentDate.HasValue || level == TimeLineTreeLevel.Minute || parentDate.Value.Minute == x.TimeComponent))
                            {
                                min = minNode.TimeComponent;

                                if (level == TimeLineTreeLevel.Minute)
                                {
                                    yield return makeTimeLineCount(minNode);
                                    continue;
                                }

                                foreach (var secNode in ((InternalTimelineTreeNode)minNode).Children.Where(
                                    x => !parentDate.HasValue || level == TimeLineTreeLevel.Second || parentDate.Value.Second == x.TimeComponent))
                                {
                                    sec = secNode.TimeComponent;

                                    if (level == TimeLineTreeLevel.Second)
                                    {
                                        yield return makeTimeLineCount(secNode);
                                        continue;
                                    }

                                    // Can't filter milliseconds!
                                    foreach (var msNode in ((InternalTimelineTreeNode)secNode).Children)
                                    {
                                        millisec = msNode.TimeComponent;
                                        yield return makeTimeLineCount(msNode);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

#if DEBUG
        /// <summary>
        /// Converts the tree to a string. Ideally only for small trees for debugging purposes
        /// </summary>
        /// <returns></returns>
        internal string DumpTree()
        {
            var result = new StringBuilder(1024);

            void DumpNode(TimelineTreeNode node, int indentLevel)
            {
                result.Append(new String(' ', indentLevel)).AppendLine(node.ToString());

                if (node is InternalTimelineTreeNode intNode)
                {
                    foreach (var child in intNode.Children)
                    {
                        DumpNode(child, indentLevel + 4);
                    }
                }
            }

            foreach (var node in _yearNodes)
            {
                DumpNode(node, 0);
            }

            return result.ToString();
        }
#endif

        /// <summary>
        /// Base class for nodes in the tree.
        /// </summary>
        private abstract class TimelineTreeNode 
        {
            protected TimelineTreeNode(short timeComponent)
            {
                TimeComponent = timeComponent;
            }

            /// <summary>
            /// The value of the time component for this Node's level in the tree.
            /// </summary>
            public short TimeComponent { get; }

            /// <summary>
            /// The number of log entries this node contains. 
            /// </summary>
            public int Count { get; set; } = 1;

            /// <summary>
            /// The position in the source file of the first event log entry
            /// covered by this node. 
            /// </summary>
            public virtual long Offset { get; }


            public override string ToString() => $"{GetType().Name}: " +
                $"TimeComponent={TimeComponent}, Count={Count}, Offset={Offset}";
        }

        /// <summary>
        /// An internal node of the tree. 
        /// </summary>
        private sealed class InternalTimelineTreeNode : TimelineTreeNode
        {
            /// <summary>
            /// Creates an instance of the InternalTimelineTreeNode class. 
            /// </summary>
            /// <param name="timeComponent">This node's time component</param>
            public InternalTimelineTreeNode(short timeComponent) : base(timeComponent)
            {
            }

            public override long Offset { get => Children[0].Offset; }

            public List<TimelineTreeNode> Children { get; } = new List<TimelineTreeNode>(8);
        }


        /// <summary>
        /// The leaf node of the tree that actually represents an entry.
        /// </summary>
        private sealed class LeafTimelineTreeNode : TimelineTreeNode
        {
            /// <summary>
            /// Creates an instance of the LeadTimelineNode class.
            /// </summary>
            /// <param name="timeComponent">This node's timecomponent</param>
            /// <param name="offset">The position in the source file of the originating 
            /// log entry. </param>
            public LeafTimelineTreeNode(short timeComponent, long offset) : base(timeComponent)
            {
                Offset = offset;
            }

            public override long Offset { get; }
        }
    }
}
