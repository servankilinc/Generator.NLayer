import { Fragment } from 'react';
import { Breadcrumb, BreadcrumbItem, BreadcrumbLink, BreadcrumbList, BreadcrumbPage, BreadcrumbSeparator } from '@/components/ui/breadcrumb';

type AppBreadcrumbProps = {
  links?: {
    href: string;
    label: string;
  }[];
  page: string;
};

export default function AppBreadcrumb({ ...props }: AppBreadcrumbProps) {
  return (
    <Breadcrumb>
      <BreadcrumbList>
        {props.links?.map((link, index) => (
          <Fragment key={`breadcrumb-link-${index}`}>
            <BreadcrumbItem>
              <BreadcrumbLink href={link.href}>{link.label}</BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator className='hidden md:block' />
          </Fragment>
        ))}
        <BreadcrumbItem>
          <BreadcrumbPage>{props.page}</BreadcrumbPage>
        </BreadcrumbItem>
      </BreadcrumbList>
    </Breadcrumb>
  );
}
