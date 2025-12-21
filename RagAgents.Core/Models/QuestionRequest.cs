using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Models
{
    public class QuestionRequest
    {
        public string Question { get; set; } = default!;
        public string ConversationId { get; set; } = default!;
        public string UserId { get; set; } = default!;
    }
}
