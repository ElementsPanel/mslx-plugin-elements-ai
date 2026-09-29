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

export interface ChatMessage {
  role: 'user' | 'assistant' | 'tool' | 'error';
  content: string;
  tool?: string;
  ok?: boolean;
  pending?: boolean;
  diff?: FileDiff;
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
  canContinue: boolean;
}

export type ChatEvent =
  | { type: 'start'; conversationId: string; messages: ChatMessage[] }
  | { type: 'message'; index: number; message: ChatMessage }
  | { type: 'delta'; index: number; content: string }
  | { type: 'reasoning'; index: number; content: string }
  | { type: 'retry'; attempt: number; maxAttempts: number; delayMs: number }
  | { type: 'progress'; tool: string; progress: { value?: number; speed?: string; fileName?: string } }
  | { type: 'done'; conversationId: string }
  | { type: 'error'; message: string };
