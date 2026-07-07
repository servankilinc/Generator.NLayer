import { useState, useRef, useEffect } from 'react';
import { Bot, Send, X, Loader2, MessageSquare, CheckCircle2, XCircle, Code2 } from 'lucide-react';
import { useAppDispatch } from '@/hooks';
import axiosHelper from '@/lib/axios-helper';
import { fetchEntities } from '@/redux/reducers/entitySlice';
import { fetchProjects } from '@/redux/reducers/projectSlice';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { toast } from 'sonner';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

interface ToolCall {
  id: string;
  pluginName: string;
  functionName: string;
  arguments: Record<string, any>;
}

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  proposedChanges?: ToolCall[];
  isPendingApproval?: boolean;
}

interface AiResponse {
  response?: string;
  requiresApproval?: boolean;
  message?: string;
  proposedChanges?: ToolCall[];
}

export default function AiChat() {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const dispatch = useAppDispatch();

  // Scroll to bottom when messages change
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isLoading, isOpen]);

  const handleSend = async (customMessage?: string) => {
    const userMsg = customMessage ?? input.trim();
    if (!userMsg) return;

    if (!customMessage) {
      setInput('');
    }
    
    // Add user message to state
    const newHistory = [...messages, { role: 'user', content: userMsg } as ChatMessage];
    setMessages(newHistory);
    setIsLoading(true);

    try {
      // Send request to API
      // We pass the history of previous messages, excluding the current one being typed
      const cleanHistory = messages.map(m => ({ role: m.role, content: m.content }));
      const response = await axiosHelper.post<AiResponse>('/generation/ai-setup', {
        prompt: userMsg,
        history: cleanHistory
      });

      if (response) {
        if (response.requiresApproval && response.proposedChanges) {
          setMessages(prev => [...prev, { 
            role: 'assistant', 
            content: response.message || 'Değişiklik onayı bekleniyor.',
            proposedChanges: response.proposedChanges,
            isPendingApproval: true
          }]);
        } else if (response.response) {
          setMessages(prev => [...prev, { role: 'assistant', content: response.response! }]);
          // Only refresh on direct response if we assume it modified things, 
          // but with the new architecture, changes happen in /execute.
          // We can leave this or remove it, depending on if the AI can still do direct modifications.
        } else {
          toast.error('AI response was empty.');
        }
      }
    } catch (error) {
      console.error(error);
      toast.error('An error occurred during communication with AI.');
      setMessages(prev => [...prev, { role: 'assistant', content: 'Üzgünüm, bir hata oluştu.' }]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleApprove = async (msgIndex: number, proposedChanges: ToolCall[]) => {
    // Mark as no longer pending
    setMessages(prev => prev.map((m, i) => i === msgIndex ? { ...m, isPendingApproval: false } : m));
    setIsLoading(true);

    try {
      await axiosHelper.post('/generation/ai-setup/execute', {
        toolCalls: proposedChanges
      });

      setMessages(prev => [...prev, { role: 'assistant', content: 'Değişiklikler başarıyla uygulandı.' }]);
      
      // Auto-refresh entities and projects
      dispatch(fetchEntities());
      dispatch(fetchProjects());
      window.dispatchEvent(new Event('ai-generation-completed'));
      toast.success('Değişiklikler uygulandı.');
      
    } catch (error) {
      console.error(error);
      toast.error('Değişiklikler uygulanırken hata oluştu.');
      setMessages(prev => [...prev, { role: 'assistant', content: 'Değişiklikler uygulanamadı. Bir hata oluştu.' }]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleReject = (msgIndex: number) => {
    // Mark as no longer pending
    setMessages(prev => prev.map((m, i) => i === msgIndex ? { ...m, isPendingApproval: false } : m));
    
    // Automatically send a rejection message to the AI
    handleSend('Kullanıcı bu değişiklikleri reddetti, lütfen farklı bir yol izle.');
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      handleSend();
    }
  };

  return (
    <>
      {/* Floating Action Button */}
      <Button
        className="fixed bottom-6 right-6 h-14 w-14 rounded-full shadow-lg z-50 p-0"
        onClick={() => setIsOpen(true)}
        style={{ display: isOpen ? 'none' : 'flex' }}
        title="Open AI Assistant"
      >
        <Bot className="h-6 w-6" />
      </Button>

      {/* Chat Window */}
      {isOpen && (
        <Card className="fixed bottom-6 right-6 w-[400px] h-[600px] shadow-2xl flex flex-col z-50 border-border bg-background transition-all duration-300 ease-in-out">
          <CardHeader className="p-4 border-b bg-card flex flex-row items-center justify-between space-y-0 rounded-t-xl">
            <CardTitle className="flex items-center gap-2 text-base">
              <Bot className="h-5 w-5 text-primary" />
              AI Assistant
            </CardTitle>
            <Button variant="ghost" size="icon" onClick={() => setIsOpen(false)} className="h-8 w-8">
              <X className="h-4 w-4" />
            </Button>
          </CardHeader>

          <CardContent className="flex-1 p-0 flex flex-col overflow-hidden">
            {/* Messages Area */}
            <div className="flex-1 overflow-y-auto p-4 space-y-4 bg-muted/10">
              {messages.length === 0 && (
                <div className="flex flex-col items-center justify-center h-full text-muted-foreground opacity-50">
                  <MessageSquare className="h-10 w-10 mb-2" />
                  <p className="text-sm">Veritabanı tasarımınız için nasıl yardımcı olabilirim?</p>
                </div>
              )}
              
              {messages.map((msg, idx) => (
                <div
                  key={idx}
                  className={`flex flex-col max-w-[90%] ${
                    msg.role === 'user' ? 'self-end items-end ml-auto' : 'self-start items-start mr-auto'
                  }`}
                >
                  <div
                    className={`px-4 py-2 rounded-2xl shadow-sm ${
                      msg.role === 'user' 
                        ? 'bg-primary text-primary-foreground rounded-br-sm' 
                        : 'bg-card text-foreground border rounded-bl-sm'
                    }`}
                  >
                    {msg.role === 'user' ? (
                      <div className="text-sm whitespace-pre-wrap break-words">{msg.content}</div>
                    ) : (
                      <div className="text-sm break-words [&_ul]:list-disc [&_ul]:pl-5 [&_ol]:list-decimal [&_ol]:pl-5 [&_p]:mb-2 last:[&_p]:mb-0 [&_h3]:font-semibold [&_h3]:mt-2 [&_h3]:mb-1 [&_pre]:p-2 [&_pre]:rounded [&_pre]:bg-muted/50 [&_pre]:overflow-x-auto [&_code]:bg-muted/50 [&_code]:px-1 [&_code]:rounded [&_code]:text-xs [&_a]:text-primary [&_a]:underline">
                        <ReactMarkdown remarkPlugins={[remarkGfm]}>
                          {msg.content}
                        </ReactMarkdown>

                        {/* Proposed Changes Card */}
                        {msg.proposedChanges && msg.proposedChanges.length > 0 && (
                          <div className="mt-3 border rounded-lg overflow-hidden bg-background">
                            <div className="bg-muted px-3 py-2 border-b flex items-center gap-2 text-xs font-semibold text-muted-foreground">
                              <Code2 className="h-4 w-4" />
                              Önerilen Değişiklikler
                            </div>
                            <div className="p-3 space-y-3">
                              {msg.proposedChanges.map((change, cIdx) => (
                                <div key={cIdx} className="text-xs">
                                  <div className="font-medium text-primary mb-1">
                                    {change.functionName}
                                  </div>
                                  <div className="bg-muted/50 rounded p-2 text-muted-foreground overflow-x-auto">
                                    {Object.entries(change.arguments).map(([key, value]) => (
                                      <div key={key} className="flex gap-2">
                                        <span className="font-medium">{key}:</span>
                                        <span>{String(value)}</span>
                                      </div>
                                    ))}
                                  </div>
                                </div>
                              ))}
                            </div>
                            
                            {/* Action Buttons */}
                            {msg.isPendingApproval ? (
                              <div className="p-3 bg-muted/30 border-t flex gap-2">
                                <Button 
                                  size="sm" 
                                  className="flex-1 bg-emerald-600 hover:bg-emerald-700 text-white"
                                  onClick={() => handleApprove(idx, msg.proposedChanges!)}
                                >
                                  <CheckCircle2 className="h-4 w-4 mr-1.5" />
                                  Onayla
                                </Button>
                                <Button 
                                  size="sm" 
                                  variant="destructive" 
                                  className="flex-1"
                                  onClick={() => handleReject(idx)}
                                >
                                  <XCircle className="h-4 w-4 mr-1.5" />
                                  Reddet
                                </Button>
                              </div>
                            ) : (
                              <div className="p-2 text-center text-xs text-muted-foreground border-t bg-muted/10">
                                Bu işlem tamamlandı veya iptal edildi.
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              ))}
              
              {isLoading && (
                <div className="flex self-start max-w-[85%]">
                  <div className="bg-card text-muted-foreground border px-4 py-3 rounded-2xl rounded-bl-sm flex items-center gap-2 shadow-sm">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    <span className="text-sm">Asistan düşünüyor...</span>
                  </div>
                </div>
              )}
              <div ref={messagesEndRef} />
            </div>

            {/* Input Area */}
            <div className="p-4 border-t bg-card mt-auto">
              <div className="flex items-center gap-2 relative">
                <Input
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyDown={handleKeyDown}
                  placeholder="Veritabanı için ne yapmak istersiniz?"
                  className="pr-12"
                  disabled={isLoading}
                  autoFocus
                />
                <Button 
                  size="icon" 
                  className="absolute right-1 top-1 bottom-1 h-auto" 
                  onClick={() => handleSend()}
                  disabled={isLoading || !input.trim()}
                >
                  <Send className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </>
  );
}
