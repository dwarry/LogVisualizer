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
    /// drill-down
    /// </summary>
    public class TimelineTree
    {
        private readonly List<TimelineTreeNode> _yearNodes = new List<TimelineTreeNode>();

        private readonly List<(DateTime, long)> _timestamps = new List<(DateTime, long)>(1000);

        public TimelineTree()
        {
        }

        public void Add(DateTime timestamp, long offset)
        {
            _timestamps.Add((timestamp, offset));
            var nodes = _yearNodes;

            foreach (TimeLineTreeLevel level in Enum.GetValues(typeof(TimeLineTreeLevel)))
            {
                var timePart = level.PartOf(timestamp);

                // Check that the timestamp has increased
                if (nodes.Count == 0 || nodes.Last().TimeComponent != timePart)
                {
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
                                n.Count++;
                                break;
                            }
                            else
                            {
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
                            nodes.Add(new LeafTimelineTreeNode(timePart, offset));
                        }
                        else
                        {
                            nodes.Add(new InternalTimelineTreeNode(timePart));
                        }
                    }
                }
                else
                {
                    nodes.Last().Count++;
                }

                if (level == TimeLineTreeLevel.Millisecond)
                {
                    break;
                }

                nodes = ((InternalTimelineTreeNode)nodes.Last()).Children;
            }
        }

        public int Count { get => _yearNodes.Sum(x => x.Count); }

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

        private abstract class TimelineTreeNode : IComparable<TimelineTreeNode>, IComparable
        {
            protected TimelineTreeNode(short timeComponent)
            {
                TimeComponent = timeComponent;
            }

            public short TimeComponent { get; }

            public int Count { get; set; } = 1;

            public virtual long Offset { get; }

            public int CompareTo(object other)
            {
                if (other == null)
                {
                    return 1;
                }

                if (other is TimelineTreeNode x)
                {
                    return CompareTo(x);
                }

                throw new ArgumentException("Unexpect value type:" + other.GetType().Name, nameof(other));
            }

            public int CompareTo(TimelineTreeNode other)
            {
                if (other == null)
                {
                    return 1;
                }

                return TimeComponent.CompareTo(other.TimeComponent);
            }

            public override string ToString() => $"{GetType().Name}: " +
                $"TimeComponent={TimeComponent}, Count={Count}, Offset={Offset}";
        }

        private sealed class InternalTimelineTreeNode : TimelineTreeNode
        {
            public InternalTimelineTreeNode(short timeComponent) : base(timeComponent)
            {
            }

            public override long Offset { get => Children[0].Offset; }

            public List<TimelineTreeNode> Children { get; } = new List<TimelineTreeNode>(8);
        }

        private sealed class LeafTimelineTreeNode : TimelineTreeNode
        {
            public LeafTimelineTreeNode(short timeComponent, long offset) : base(timeComponent)
            {
                Offset = offset;
            }

            public override long Offset { get; }
        }
    }
}
