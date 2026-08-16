import type { Preview } from '@storybook/react-vite';
import '../src/index.css';
import './storybook.css';

const fixedNow = new Date('2026-08-16T12:00:00.000Z').valueOf();
const OriginalDate = Date;

class FixedDate extends OriginalDate {
  constructor(...args: ConstructorParameters<typeof OriginalDate>) {
    super(...(args.length === 0 ? [fixedNow] : args));
  }

  static now() {
    return fixedNow;
  }
}

Object.setPrototypeOf(FixedDate, OriginalDate);
globalThis.Date = FixedDate as DateConstructor;

Math.random = () => 0.37;

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    a11y: {
      test: 'todo',
    },
    options: {
      storySort: {
        order: ['Components', 'Screens'],
      },
    },
  },
};

export default preview;
