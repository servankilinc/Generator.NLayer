import { createAsyncThunk, createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type AppSetting from '@/models/appsetting/appsetting';
import type Project from '@/models/project/project';
import axiosHelper from '@/lib/axios-helper';
import { toast } from 'sonner';

interface StateUI {
  projects: Project[];
  loading: boolean;
  activeProject: Project | null;
  appSettings: AppSetting | null;
}

const initialState: StateUI = {
  projects: [],
  loading: false,
  activeProject: null,
  appSettings: null
};

export const projectSlice = createSlice({
  name: 'projectSlice',
  initialState,
  reducers: {
    setProjects: (state, action: PayloadAction<Project[]>) => {
      state.projects = action.payload;
    },
    setActiveProject: (state, action: PayloadAction<Project>) => {
      state.activeProject = action.payload;
    },
    setAppSettings: (state, action: PayloadAction<AppSetting>) => {
      state.appSettings = action.payload;
    }
  },
  extraReducers: builder => {
    builder
      .addCase(fetchProjects.pending, state => {
        state.loading = true;
      })
      .addCase(fetchProjects.fulfilled, (state, action) => {
        state.loading = false;
        state.projects = action.payload ?? [];
      });
  }
});

export const fetchProjects = createAsyncThunk('fetchProjects', async () => {
  try {
    const response = await axiosHelper.get<Project[]>('/project/list');
    return response;
  } catch {
    toast.error('Projects Could not Readed!');
  }
});

export const { setProjects, setActiveProject, setAppSettings } = projectSlice.actions;
export default projectSlice.reducer;
