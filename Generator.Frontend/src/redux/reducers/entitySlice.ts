import { createAsyncThunk, createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type Entity from '@/models/entity/entity';
import axiosHelper from '@/lib/axios-helper';
import { toast } from 'sonner';

interface StateUI {
  entities: Entity[];
  loading: boolean;
}

const initialState: StateUI = {
  entities: [],
  loading: false
};

export const entitySlice = createSlice({
  name: 'entitySlice',
  initialState,
  reducers: {
    setEntities: (state, action: PayloadAction<Entity[]>) => {
      state.entities = action.payload;
    }
  },
  extraReducers: builder => {
    builder
      .addCase(fetchEntities.pending, state => {
        state.loading = true;
      })
      .addCase(fetchEntities.fulfilled, (state, action) => {
        state.loading = false;
        state.entities = action.payload ?? [];
      });
  }
});

export const fetchEntities = createAsyncThunk('fetchEntities', async () => {
  try {
    const response = await axiosHelper.get<Entity[]>('/entity/list/withBaseFields');
    return response;
  } catch {
    toast.error('Entities Could not Readed!');
  }
});

export const { setEntities } = entitySlice.actions;
export default entitySlice.reducer;
