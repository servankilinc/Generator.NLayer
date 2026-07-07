import { createRoot } from 'react-dom/client';
import { store } from '@/redux/store.ts';
import { Provider } from 'react-redux';
import { createBrowserRouter } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import BaseLayout from '@/layouts/base';
import '@/styles/index.css';
import Home from '@/views/home';
import Projects from '@/views/project';
import Entities from '@/views/entity/index';
import Dtos from '@/views/dto/index';
import Diagram from '@/views/diagram/index';
import DtoDiagram from '@/views/diagram/dto-diagram';
import Generation from '@/views/generation';

const router = createBrowserRouter([
  {
    path: '/',
    Component: BaseLayout,
    children: [
      { index: true, Component: Home },
      { path: 'projects', Component: Projects },
      { path: 'entities', Component: Entities },
      { path: 'dtos/:entityId', Component: Dtos },
      { path: 'diagram', Component: Diagram },
      { path: 'diagram/dtos/:entityId', Component: DtoDiagram },
      { path: 'generation', Component: Generation }
    ]
  }
]);

createRoot(document.getElementById('root')!).render(
  <Provider store={store}>
    <RouterProvider router={router} />
  </Provider>
);
