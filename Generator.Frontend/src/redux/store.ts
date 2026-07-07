import { configureStore } from '@reduxjs/toolkit';
import themeReducer from '@/redux/reducers/themeSlice';
import projectReducer from '@/redux/reducers/projectSlice';
import entityReducer from '@/redux/reducers/entitySlice';

export const store = configureStore({
  reducer: {
    theme: themeReducer,
    project: projectReducer,
    entity: entityReducer
  }
});

// Infer the `RootState` and `AppDispatch` types from the store itself
export type RootState = ReturnType<typeof store.getState>;
// Inferred type: {posts: PostsState, comments: CommentsState, users: UsersState}
export type AppDispatch = typeof store.dispatch;
export type AppStore = typeof store;
