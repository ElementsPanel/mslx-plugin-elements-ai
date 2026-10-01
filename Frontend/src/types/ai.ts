export type PermissionMode = 'default' | 'full';

export interface ChatPreferences {
  sendOnEnter: boolean;
}

export interface FileDiff {
  path: string;
  patch: string;
  truncated: boolean;
}

export interface ToolApproval {
  id: string;
  arguments: string;
}

export interface ToolQuestion {
  id: string;
  question: string;
  options: string[];
}

export interface InteractionStatus {
  active: boolean;
  approvalIds: string[];
  questionIds: string[];
}

export interface AiTaskProgress {
  taskId: string;
  title: string;
  state: 'pending' | 'running' | 'success' | 'failed' | 'canceled';
  value: number | null;
  message: string;
  fileName?: string;
  speed?: string;
  instanceId?: number;
  completed: boolean;
  success: boolean;
}

export interface TaskProgressView extends AiTaskProgress {
  unavailable?: boolean;
}

export interface ChatMessage {
  role: 'user' | 'assistant' | 'tool' | 'error';
  content: string;
  tool?: string;
  ok?: boolean;
  pending?: boolean;
  diff?: FileDiff;
  taskProgress?: AiTaskProgress;
  commandResult?: {
    nodeId: string;
    workingDirectory: string;
    exitCode: number;
    stdout: string;
    stderr: string;
    timedOut: boolean;
    truncated: boolean;
  };
  approval?: ToolApproval;
  question?: ToolQuestion;
  reasoning?: string;
  reasoningComplete?: boolean;
  workComplete?: boolean;
}

export interface ModelOption {
  id: string;
  name: string;
  model: string;
  source: 'preset' | 'personal';
  endpoint?: string;
  hasApiKey?: boolean;
  thinkingEnabled: boolean | null;
  thinkingEffort: 'low' | 'medium' | 'high';
}

export interface ModelInput {
  id?: string;
  name: string;
  endpoint: string;
  model: string;
  apiKey: string;
  clearApiKey: boolean;
  thinkingEnabled: boolean | null;
  thinkingEffort: 'low' | 'medium' | 'high';
}

export interface AiStatus {
  ready: boolean;
  admin: boolean;
  userId: string;
  models: ModelOption[];
  preferences: ChatPreferences;
}

export interface ConversationSummary {
  id: string;
  title: string;
  modelId: string;
  modelName: string;
  updatedAt: number;
}

export interface ConversationDetail extends ConversationSummary {
  messages: ChatMessage[];
}

export type ChatEvent =
  | { type: 'start'; conversationId: string; messages: ChatMessage[] }
  | { type: 'message'; index: number; message: ChatMessage }
  | { type: 'delta'; index: number; content: string }
  | { type: 'reasoning'; index: number; content: string }
  | { type: 'retry'; attempt: number; maxAttempts: number; delayMs: number }
  | { type: 'progress'; tool: string; progress: AiTaskProgress }
  | { type: 'done'; conversationId: string }
  | { type: 'error'; message: string };
