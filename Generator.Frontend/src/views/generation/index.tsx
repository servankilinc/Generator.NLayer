import { useCallback, useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import axiosHelper from '@/lib/axios-helper';
import { toast } from 'sonner';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Play, Square, Terminal, Trash2, ArrowDown } from 'lucide-react';

type GenerationStatus = 'idle' | 'connecting' | 'running' | 'completed' | 'error';

export default function Generation() {
  const [status, setStatus] = useState<GenerationStatus>('idle');
  const [logs, setLogs] = useState<string[]>([]);
  const [progress, setProgress] = useState(0);
  const [autoScroll, setAutoScroll] = useState(true);

  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const logBoxRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = useCallback(() => {
    if (logBoxRef.current && autoScroll) {
      logBoxRef.current.scrollTop = logBoxRef.current.scrollHeight;
    }
  }, [autoScroll]);

  useEffect(() => {
    scrollToBottom();
  }, [logs, scrollToBottom]);

  const startConnection = useCallback(async () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5092/comunication-hub')
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on('AppendToResults', (message: string) => {
      setLogs(prev => [...prev, message]);
    });

    connection.on('Progress', (rate: number) => {
      setProgress(rate);
      if (rate >= 100) {
        setStatus('completed');
      }
    });

    connection.onclose(() => {
      if (status === 'running') {
        setStatus('error');
        toast.error('SignalR connection lost');
      }
    });

    connection.onreconnecting(() => {
      toast.warning('Reconnecting to server...');
    });

    connection.onreconnected(() => {
      toast.success('Reconnected to server');
    });

    try {
      await connection.start();
      connectionRef.current = connection;
    } catch {
      toast.error('Failed to connect to SignalR hub');
      setStatus('error');
    }
  }, [status]);

  const handleStartGeneration = async () => {
    setStatus('connecting');
    setLogs([]);
    setProgress(0);

    try {
      await startConnection();
      setStatus('running');
      await axiosHelper.get('/start-generate');
      setLogs(prev => [...prev, '▶ Generation triggered successfully']);
    } catch {
      toast.error('Failed to start generation');
      setStatus('error');
    }
  };

  const handleStop = async () => {
    if (connectionRef.current) {
      await connectionRef.current.stop();
      connectionRef.current = null;
    }
    setStatus('idle');
    toast.info('Generation stopped');
  };

  const handleClearLogs = () => {
    setLogs([]);
    setProgress(0);
    setStatus('idle');
  };

  useEffect(() => {
    return () => {
      connectionRef.current?.stop();
    };
  }, []);

  const statusConfig: Record<GenerationStatus, { label: string; color: string; dot: string }> = {
    idle: { label: 'Ready', color: 'text-muted-foreground', dot: 'bg-muted-foreground' },
    connecting: { label: 'Connecting…', color: 'text-amber-500', dot: 'bg-amber-500' },
    running: { label: 'Running', color: 'text-emerald-500', dot: 'bg-emerald-500' },
    completed: { label: 'Completed', color: 'text-blue-500', dot: 'bg-blue-500' },
    error: { label: 'Error', color: 'text-destructive', dot: 'bg-destructive' }
  };

  const currentStatus = statusConfig[status];

  return (
    <div className='flex flex-1 flex-col gap-4 p-4 pt-0'>
      {/* Header Card */}
      <Card>
        <CardHeader className='border-b'>
          <div className='flex items-center justify-between'>
            <div>
              <CardTitle className='flex items-center gap-2 text-lg'>
                <Terminal className='size-5' />
                Code Generation
              </CardTitle>
              <CardDescription className='mt-1'>Trigger and monitor the code generation process in real-time</CardDescription>
            </div>
            <div className='flex items-center gap-2'>
              {/* Status Indicator */}
              <div className={`flex items-center gap-2 rounded-lg border px-3 py-1.5 text-xs font-medium ${currentStatus.color}`}>
                <span className={`inline-block size-2 rounded-full ${currentStatus.dot} ${status === 'running' || status === 'connecting' ? 'animate-pulse' : ''}`} />
                {currentStatus.label}
              </div>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className='flex items-center gap-3'>
            {status === 'running' || status === 'connecting' ? (
              <Button variant='destructive' onClick={handleStop} id='btn-stop-generation'>
                <Square className='size-4' data-icon='inline-start' />
                Stop
              </Button>
            ) : (
              <Button onClick={handleStartGeneration} id='btn-start-generation'>
                <Play className='size-4' data-icon='inline-start' />
                Start Generation
              </Button>
            )}
            <Button variant='outline' onClick={handleClearLogs} disabled={status === 'running'} id='btn-clear-logs'>
              <Trash2 className='size-4' data-icon='inline-start' />
              Clear
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Progress Card */}
      <Card>
        <CardContent className='pt-1'>
          <div className='flex items-center justify-between mb-2'>
            <span className='text-sm font-medium text-muted-foreground'>Progress</span>
            <span className='text-sm font-semibold tabular-nums'>{Math.round(progress)}%</span>
          </div>
          <div className='relative h-3 w-full overflow-hidden rounded-full bg-secondary'>
            <div
              className='h-full rounded-full transition-all duration-500 ease-out'
              style={{
                width: `${progress}%`,
                background:
                  progress >= 100
                    ? 'linear-gradient(90deg, oklch(0.65 0.19 145), oklch(0.72 0.17 155))'
                    : 'linear-gradient(90deg, oklch(0.55 0.2 260), oklch(0.65 0.22 280))'
              }}
            />
            {(status === 'running' || status === 'connecting') && (
              <div className='absolute inset-0 overflow-hidden rounded-full'>
                <div
                  className='animate-pulse h-full rounded-full opacity-30'
                  style={{
                    width: `${progress}%`,
                    background: 'linear-gradient(90deg, transparent, oklch(0.85 0.15 270), transparent)'
                  }}
                />
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Log Output Card */}
      <Card className='flex-1 min-h-0 flex flex-col'>
        <CardHeader className='border-b'>
          <div className='flex items-center justify-between'>
            <CardTitle className='flex items-center gap-2 text-sm'>
              <Terminal className='size-4' />
              Output
              {logs.length > 0 && <span className='rounded-md bg-muted px-1.5 py-0.5 text-xs text-muted-foreground tabular-nums'>{logs.length} lines</span>}
            </CardTitle>
            <Button
              variant={autoScroll ? 'secondary' : 'ghost'}
              size='icon-xs'
              onClick={() => setAutoScroll(prev => !prev)}
              title={autoScroll ? 'Auto-scroll ON' : 'Auto-scroll OFF'}
              id='btn-toggle-autoscroll'>
              <ArrowDown className='size-3' />
            </Button>
          </div>
        </CardHeader>
        <CardContent className='flex-1 min-h-0 pt-0'>
          <div
            ref={logBoxRef}
            className='mt-3 h-[calc(100vh-460px)] min-h-48 overflow-y-auto rounded-lg border bg-[oklch(0.13_0_0)] p-4 font-mono text-[13px] leading-relaxed text-emerald-400 selection:bg-emerald-400/30'>
            {logs.length === 0 ? (
              <div className='flex h-full items-center justify-center text-muted-foreground/50'>
                <div className='text-center'>
                  <Terminal className='mx-auto mb-2 size-8 opacity-40' />
                  <p className='text-sm'>Waiting for generation output…</p>
                </div>
              </div>
            ) : (
              logs.map((log, index) => (
                <div key={index} className='flex gap-3 hover:bg-white/[0.03] rounded px-1 -mx-1'>
                  <span className='select-none text-muted-foreground/40 tabular-nums'>{String(index + 1).padStart(4, '\u00A0')}</span>
                  <span className='whitespace-pre-wrap break-all'>{log}</span>
                </div>
              ))
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
