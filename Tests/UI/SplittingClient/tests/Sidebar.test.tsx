import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { Sidebar } from '../src/components/Sidebar';

describe('Sidebar', () => {
    it('renders three navigation links', () => {
        render(<MemoryRouter><Sidebar open={true} /></MemoryRouter>);
        expect(screen.getByRole('link', { name: /orders/i })).toHaveAttribute('href', '/orders');
        expect(screen.getByRole('link', { name: /goods/i })).toHaveAttribute('href', '/goods');
        expect(screen.getByRole('link', { name: /users/i })).toHaveAttribute('href', '/users');
    });

    it('toggles sidebar--open class based on open prop', () => {
        const { rerender } = render(<MemoryRouter><Sidebar open={false} /></MemoryRouter>);
        expect(screen.getByTestId('sidebar').className).not.toContain('sidebar--open');
        rerender(<MemoryRouter><Sidebar open={true} /></MemoryRouter>);
        expect(screen.getByTestId('sidebar').className).toContain('sidebar--open');
    });
});
